using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Xml;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.CoreImpl;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// <summary>
	///
	/// </summary>
	public class InterlinearTestBase : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		/// <summary>
		/// This is used to record the information we (may) want to verify about a part of a paragraph.
		/// </summary>
		internal interface IMockStTxtParaAnnotation
		{
			int Hvo { get; set; }
			int BeginOffset { get; set; }
		}

		abstract class MockStTxtParaAnnotation : IMockStTxtParaAnnotation, ICloneable
		{
			#region IMockStTxtParaAnnotation Members

			public int Hvo { get; set; }

			public int BeginOffset { get; set; }

			#endregion

			internal void SetMockStTxtParaAnnotationCoreProperties(int hvo, int beginOffset)
			{
				Hvo = hvo;
				BeginOffset = beginOffset;
			}

			#region ICloneable Members

			public object Clone()
			{
				return this.MemberwiseClone();
			}

			#endregion
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="fileRelativePath"></param>
		/// <returns></returns>
		protected string ConfigurationFilePath(string fileRelativePath)
		{
			return Path.Combine(FwDirectoryFinder.SourceDirectory, fileRelativePath);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="textsDefinitionRelativePath"></param>
		/// <param name="index"></param>
		/// <param name="textsDefn"></param>
		/// <returns></returns>
		protected FDO.IText LoadTestText(string textsDefinitionRelativePath, int index, XmlDocument textsDefn)
		{
			TextBuilder tb;
			return LoadTestText(textsDefinitionRelativePath, index, textsDefn, out tb);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="textsDefinitionRelativePath"></param>
		/// <param name="index"></param>
		/// <param name="textsDefn"></param>
		/// <param name="tb"></param>
		/// <returns></returns>
		protected FDO.IText LoadTestText(string textsDefinitionRelativePath, int index, XmlDocument textsDefn, out TextBuilder tb)
		{
			string textsDefinitionsPath = ConfigurationFilePath(textsDefinitionRelativePath);
			textsDefn.Load(textsDefinitionsPath);
			XmlNode text1Defn = textsDefn.SelectSingleNode("/Texts6001/Text[" + index + "]");
			tb = new ParagraphParserTests.TextBuilder(Cache);
			return tb.BuildText(text1Defn);
		}

		/// <summary>
		/// Given the internal kind of node list that XmlDocuments use, convert it to a regular list of XmlNodes.
		/// </summary>
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
		/// This abstract class can be used to validate any paragraph structure against an existing IStTxtPara structure.
		/// </summary>
		abstract public class ParagraphValidator
		{
			/// <summary>
			///
			/// </summary>
			protected ParagraphValidator()
			{
			}

			/// <summary>
			///
			/// </summary>
			/// <param name="expectedParaInfo"></param>
			/// <param name="actualParaInfo"></param>
			virtual public void ValidateParagraphs(XmlNode expectedParaInfo, IStTxtPara actualParaInfo)
			{
				string paraContext = String.Format("Para({0})", GetParagraphContext(expectedParaInfo));
				ValidateParagraphSegments(expectedParaInfo, actualParaInfo, paraContext);
			}

			/// <summary>
			/// Validate the actual analysis of the paragraph against what is expected, indicated by the expectedParaInfo,
			/// which is an XmlNode from our test data file for an StTxtPara.
			/// </summary>
			/// <param name="expectedParaInfo"></param>
			/// <param name="actualParaInfo"></param>
			/// <param name="paraContext"></param>
			virtual protected void ValidateParagraphSegments(XmlNode expectedParaInfo, IStTxtPara actualParaInfo, string paraContext)
			{
				var expectedSegments = GetExpectedSegments(expectedParaInfo);
				var actualSegments = actualParaInfo.SegmentsOS.ToList();
				// Validate Segments
				Assert.AreEqual(expectedSegments.Count, actualSegments.Count,
					String.Format("Expect the same Segment count in {0}.", paraContext));
				using (var actualSegEnumerator = actualSegments.GetEnumerator())
				{
					int iSegment = 0;
					foreach (XmlNode expectedSegment in expectedSegments)
					{
						string segmentContext = String.Format(paraContext + "/Segment({0})", iSegment);
						actualSegEnumerator.MoveNext();
						ISegment actualSegment = actualSegEnumerator.Current;
						ValidateSegmentOuterElements(expectedSegment, actualSegment, segmentContext);
						ValidateSegmentSegForms(expectedSegment, actualSegment, segmentContext);
						iSegment++;
					}
				}
			}

			/// <summary>
			///
			/// </summary>
			/// <param name="expectedSegment"></param>
			/// <param name="actualSegment"></param>
			/// <param name="segmentContext"></param>
			virtual protected void ValidateSegmentOuterElements(object expectedSegment, ISegment actualSegment, string segmentContext)
			{
				// base override
				return;
			}

			/// <summary>
			///
			/// </summary>
			/// <param name="expectedSegment"></param>
			/// <param name="actualSegment"></param>
			/// <param name="segmentContext"></param>
			virtual protected void ValidateSegmentSegForms(object expectedSegment, ISegment actualSegment, string segmentContext)
			{
				ArrayList expectedSegForms = GetExpectedSegmentForms(expectedSegment);
				IList<IAnalysis> actualSegForms = GetActualSegmentForms(actualSegment);
				// Validate Segments
				Assert.AreEqual(expectedSegForms.Count, actualSegForms.Count,
					String.Format("Expect the same SegmentForm count in {0}.", segmentContext));
				using (IEnumerator<IAnalysis> actualSegFormEnumerator = actualSegForms.GetEnumerator())
				{
					int iSegForm = 0;
					foreach (object expectedSegForm in expectedSegForms)
					{
						string segFormContext = String.Format(segmentContext + "/SegForm({0})", iSegForm);
						actualSegFormEnumerator.MoveNext();
						IAnalysis actualSegForm = actualSegFormEnumerator.Current;
						ValidateSegFormOuterElements(expectedSegForm, actualSegForm, segFormContext);
						ValidateSegForms(expectedSegForm, actualSegForm, segFormContext);
						iSegForm++;
					}
				}
			}

			/// <summary>
			///
			/// </summary>
			/// <param name="expectedSegForm"></param>
			/// <param name="actualSegForm"></param>
			/// <param name="segFormContext"></param>
			virtual protected void ValidateSegFormOuterElements(object expectedSegForm, IAnalysis actualSegForm, string segFormContext)
			{
				return;
			}

			/// <summary>
			///
			/// </summary>
			/// <param name="expectedSegForm"></param>
			/// <param name="actualSegForm"></param>
			/// <param name="segFormContext"></param>
			virtual protected void ValidateSegForms(object expectedSegForm, IAnalysis actualSegForm, string segFormContext)
			{
				return;
			}

			/// <summary>
			///
			/// </summary>
			/// <param name="expectedParaInfo"></param>
			/// <returns></returns>
			abstract protected string GetParagraphContext(object expectedParaInfo);

			/// <summary>
			///
			/// </summary>
			/// <param name="expectedParaInfo"></param>
			/// <returns></returns>
			abstract protected List<XmlNode> GetExpectedSegments(XmlNode expectedParaInfo);
			/// <summary>
			///
			/// </summary>
			/// <param name="expectedSegment"></param>
			/// <returns></returns>
			abstract protected ArrayList GetExpectedSegmentForms(object expectedSegment);
			/// <summary>
			///
			/// </summary>
			/// <param name="actualSegment"></param>
			/// <returns></returns>
			abstract protected IList<IAnalysis> GetActualSegmentForms(ISegment actualSegment);
		}

		/// <summary>
		///
		/// </summary>
		protected class FdoValidator : ParagraphValidator
		{
			/// <summary>
			///
			/// </summary>
			protected FdoCache m_cache = null;
			/// <summary>
			///
			/// </summary>
			protected IStTxtPara m_para = null;

			internal FdoValidator(IStTxtPara para)
			{
				m_para = para;
				m_cache = m_para.Cache;
			}

			/// <summary>
			/// Verify that the text actually in the paragraph for the indicated segment and form
			/// is what is expected.
			/// </summary>
			/// <param name="tapb"></param>
			/// <param name="iSegment"></param>
			/// <param name="iSegForm"></param>
			internal static void ValidateCbaWordToBaselineWord(ParagraphAnnotatorForParagraphBuilder tapb, int iSegment, int iSegForm)
			{
				int ws;
				ITsString tssStringValue = GetTssStringValue(tapb, iSegment, iSegForm, out ws);
				IAnalysis analysis = tapb.GetAnalysis(iSegment, iSegForm);
				IWfiWordform wfInstanceOf = analysis.Wordform;
				ITsString tssWf = wfInstanceOf.Form.get_String(ws);
				string locale = wfInstanceOf.Services.WritingSystemManager.Get(ws).IcuLocale;
				var cf = new CaseFunctions(locale);
				string context = String.Format("[{0}]", tssStringValue);
				const string msg = "{0} cba mismatch in {1}.";
				Assert.AreEqual(cf.ToLower(tssStringValue.Text), cf.ToLower(tssWf.Text),
									String.Format(msg, "underlying wordform for InstanceOf", context));
			}

			internal static ITsString GetTssStringValue(ParagraphAnnotatorForParagraphBuilder tapb, int iSegment, int iSegForm, out int ws)
			{
				ITsString tssStringValue = tapb.GetSegment(iSegment).GetBaselineText(iSegForm);
				ws = TsStringUtils.GetWsAtOffset(tssStringValue, 0);
				return tssStringValue;
			}

			/// <summary>
			///
			/// </summary>
			/// <param name="expectedParaInfo"></param>
			/// <returns></returns>
			protected override string GetParagraphContext(object expectedParaInfo)
			{
				throw new Exception("The method or operation is not implemented.");
			}

			/// <summary>
			///
			/// </summary>
			/// <param name="expectedParaInfo"></param>
			/// <returns></returns>
			protected override List<XmlNode> GetExpectedSegments(XmlNode expectedParaInfo)
			{
				throw new Exception("The method or operation is not implemented.");
			}

			/// <summary>
			///
			/// </summary>
			/// <param name="expectedSegment"></param>
			/// <returns></returns>
			protected override ArrayList GetExpectedSegmentForms(object expectedSegment)
			{
				throw new Exception("The method or operation is not implemented.");
			}

			/// <summary>
			///
			/// </summary>
			/// <param name="actualSegment"></param>
			/// <returns></returns>
			protected override IList<IAnalysis> GetActualSegmentForms(ISegment actualSegment)
			{
				throw new Exception("The method or operation is not implemented.");
			}
		}

		/// <summary>
		/// This validates the actual IStTxtPara against an expected Xml paragraph structure that is based on the conceptual model.
		/// </summary>
		protected class ConceptualModelXmlParagraphValidator : FdoValidator
		{
			ParagraphBuilder m_pb = null;

			internal ConceptualModelXmlParagraphValidator(ParagraphBuilder pb) : base (pb.ActualParagraph)
			{
				m_pb = pb;
			}

			/// <summary>
			///
			/// </summary>
			/// <param name="expectedParaInfo"></param>
			/// <returns></returns>
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


			/// <summary>
			/// Get a list of the XmlNodes (CmBaseAnnotations in the Segments16 property) of the input XmlNode,
			/// which represents an StTxtPara in our test data.
			/// </summary>
			override protected List<XmlNode> GetExpectedSegments(XmlNode expectedParaInfo)
			{
				return ParagraphBuilder.SegmentNodes(expectedParaInfo);
			}

			/// <summary>
			///
			/// </summary>
			/// <param name="expectedSegment"></param>
			/// <returns></returns>
			override protected ArrayList GetExpectedSegmentForms(object expectedSegment)
			{
				return new ArrayList(ParagraphBuilder.SegmentFormNodes(expectedSegment as XmlNode).ToArray());
			}

			/// <summary>
			///
			/// </summary>
			/// <param name="actualSegment"></param>
			/// <returns></returns>
			override protected IList<IAnalysis> GetActualSegmentForms(ISegment actualSegment)
			{
				return actualSegment.AnalysesRS.ToList();
			}

			private void CompareAnalyses(object expectedNode, IAnalysis actualAnalysis, string context)
			{
				var node = expectedNode as XmlNode;
				int hvoAnalysisExpected = ParagraphBuilder.GetAnalysisId(node);
				if (hvoAnalysisExpected == -1)
				{
					// default: no special analysis set, should have made a wordform or punctform with the same form as the node.
					IAnalysis analysis = (IAnalysis)m_cache.ServiceLocator.GetObject(actualAnalysis.Hvo);
					string expectedText = node.SelectSingleNode("StringValue37").InnerText;
					Assert.AreEqual(expectedText, analysis.GetForm(TsStringUtils.GetWsAtOffset(m_para.Contents, 0)).Text, context);
					string guid = ParagraphBuilder.GetAnnotationTypeGuid(node);
					if (guid == ParagraphBuilder.WficGuid)
						Assert.IsTrue(analysis is IWfiWordform, "default parse should produce wordform here " + context);
					else
						Assert.IsTrue(analysis is IPunctuationForm, "default parse should produce  punctuation form here " + context);
				}
				else
				{
					Assert.AreEqual(hvoAnalysisExpected, actualAnalysis.Hvo, context);
				}
			}

			/// <summary>
			/// Validate the "outer" information of an XmlNode representing the expected segment.
			/// That is basically its attributes, which currently just means its beginOffset.
			/// Note that these offsets are not stored in the file, but (todo!) generated while assembling the paragraph.
			/// </summary>
			protected override void ValidateSegmentOuterElements(object expectedSegment, ISegment actualSegment, string segmentContext)
			{
				XmlNode expected = expectedSegment as XmlNode; // generic argument, but this subclass always uses XmlNode for expected.
				int beginOffset = XmlUtils.GetMandatoryIntegerAttributeValue(expected, "beginOffset");
				Assert.AreEqual(beginOffset, actualSegment.BeginOffset);

			}

			/// <summary>
			/// This awkwardly named method fits into a pattern of names for validating the outer (XML attributes) and inner
			/// (XML content) parts of an XML representation of an expected value. In this case, for an IAnalysis, the only
			/// thing we can verify is that it is the right object.
			/// </summary>
			protected override void ValidateSegFormOuterElements(object expectedSegForm, IAnalysis actualSegForm, string segFormContext)
			{
				CompareAnalyses(expectedSegForm, actualSegForm, segFormContext);
			}

			internal void ValidateActualParagraphAgainstDefn()
			{
				ITsString paraContents = m_pb.GenerateParaContentFromAnnotations();
				Assert.AreEqual(paraContents.Text, m_pb.ActualParagraph.Contents.Text,
								"Expected edited text to be the same as text built from defn.");
				base.ValidateParagraphs(m_pb.ParagraphDefinition, m_pb.ActualParagraph);
			}
		}

#pragma warning disable 1591
		/// <summary>
		/// This class allows annotating wordforms in a paragraph.
		/// </summary>
		public class ParagraphAnnotator
		{
			protected IStTxtPara m_para = null;
			protected FdoCache m_cache = null;
			protected bool m_fNeedReparseParagraph = false;

			public ParagraphAnnotator(IStTxtPara para)
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

			public void ReparseParagraph()
			{
				ParagraphParser.ParseParagraph(m_para, true, true);
				NeedReparseParagraph = false;
			}

			/// <summary>
			/// create a variant and link it to the given leMain entry
			/// and confirm this analysis on the given (monomorphemic) cba.
			/// </summary>
			/// <param name="iSegment"></param>
			/// <param name="iSegForm"></param>
			/// <param name="leMain"></param>
			/// <param name="variantType"></param>
			/// <returns>hvo of the resulting LexEntryRef</returns>
			virtual public ILexEntryRef SetVariantOf(int iSegment, int iSegForm, ILexEntry leMain, ILexEntryType variantType)
			{
				if (variantType == null)
					throw new ArgumentNullException("requires non-null variantType parameter.");
				// for now, just create the variant entry and the variant of target, treating the wordform as monomorphemic.
				ITsString tssVariantLexemeForm = GetBaselineText(iSegment, iSegForm);
				ILexEntryRef ler = leMain.CreateVariantEntryAndBackRef(variantType, tssVariantLexemeForm);
				ILexEntry variant = ler.Owner as ILexEntry;
				ArrayList morphs = new ArrayList(1);
				morphs.Add(variant.LexemeFormOA);
				BreakIntoMorphs(iSegment, iSegForm, morphs);
				ILexEntry mainEntry;
				ILexSense mainSense;
				MorphServices.GetMainEntryAndSenseStack(ler.ComponentLexemesRS.First() as IVariantComponentLexeme, out mainEntry, out mainSense);
				SetMorphSense(iSegment, iSegForm, 0, mainSense);
				return ler;
			}

			virtual internal IWfiWordform SetAlternateCase(string wordform, int iOccurrenceInParagraph, StringCaseStatus targetState)
			{
				return null; // override
			}

			virtual public IWfiWordform SetAlternateCase(int iSegment, int iSegForm, StringCaseStatus targetState, out string alternateCaseForm)
			{
				// Get actual segment form.
				var analysisActual = GetAnalysis(iSegment, iSegForm);
				int hvoActualInstanceOf;
				IWfiWordform actualWordform;
				GetRealWordformInfo(analysisActual, out hvoActualInstanceOf, out actualWordform);
				ITsString tssWordformBaseline = GetBaselineText(iSegment, iSegForm);
				// Add any relevant 'other case' forms.
				int nvar;
				int ws = tssWordformBaseline.get_Properties(0).GetIntPropValues((int)FwTextPropType.ktptWs, out nvar);
				string locale = m_cache.ServiceLocator.WritingSystemManager.Get(ws).IcuLocale;
				var cf = new CaseFunctions(locale);
				switch (targetState)
				{
					case StringCaseStatus.allLower:
						alternateCaseForm = cf.ToLower(actualWordform.Form.get_String(ws).Text);
						break;
					default:
						throw new ArgumentException("target StringCaseStatus(" + targetState + ") not yet supported.");
				}

				// Find or create the new wordform.
				IWfiWordform wfAlternateCase = WfiWordformServices.FindOrCreateWordform(m_cache, TsStringUtils.MakeTss(alternateCaseForm, ws));

				// Set the annotation to this wordform.
				SetAnalysis(iSegment, iSegForm, wfAlternateCase);
				return wfAlternateCase;
			}

			virtual internal IWfiGloss SetDefaultWordGloss(string wordform, int iOccurrenceInParagraph)
			{
				return null;	// override
			}

			virtual internal IWfiGloss SetDefaultWordGloss(int iSegment, int iSegForm, out string gloss)
			{
				return SetDefaultWordGloss(iSegment, iSegForm, null, out gloss);
			}


			virtual internal IWfiGloss SetDefaultWordGloss(int iSegment, int iSegForm, IWfiAnalysis actualWfiAnalysis, out string gloss)
			{
				gloss = "";
				// Get actual segment form.
				var analysisActual = GetAnalysis(iSegment, iSegForm);

				// Get the wordform for this segmentForm
				ITsString tssWordformBaseline = GetBaselineText(iSegment, iSegForm);

				// Find or create the current analysis of the actual annotation.
				if (actualWfiAnalysis == null)
				{
					actualWfiAnalysis = FindOrCreateWfiAnalysis(analysisActual);
				}
				// Make a new gloss based upon the wordform and segmentForm path.
				var newGloss = m_cache.ServiceLocator.GetInstance<IWfiGlossFactory>().Create();
				// Add the gloss to the WfiAnalysis.
				actualWfiAnalysis.MeaningsOC.Add(newGloss);
				gloss = String.Format("{0}.{1}.{2}", iSegment, iSegForm, tssWordformBaseline.Text);
				newGloss.Form.set_String(m_cache.DefaultAnalWs, gloss);

				// Set the new expected and actual analysis.
				SetAnalysis(iSegment, iSegForm, newGloss);
				return newGloss;
			}

			/// <summary>
			/// Set the analysis at the specified segment, index.
			/// Enhance JohnT: may need it to fill in prior segments and/or annotations.
			/// </summary>
			private void SetAnalysis(int iSegment, int iSegForm, IAnalysis newGloss)
			{
				m_para.SegmentsOS[iSegment].AnalysesRS.Replace(iSegForm, 1, new ICmObject[] {newGloss});
			}


			/// <summary>
			/// Walk through the paragraph and create word glosses (formatted: wordform.segNum.segFormNum) and segment annotations.
			/// </summary>
			/// <returns>list of wordglosses for each analysis in the paragraph, including non-wordforms (ie. hvo == 0).</returns>
			internal protected virtual IList<IWfiGloss> SetupDefaultWordGlosses()
			{
				int iseg = 0;
				IList<IWfiGloss> wordGlosses = new List<IWfiGloss>();
				foreach (var seg in m_para.SegmentsOS)
				{
					int isegform = 0;
					foreach (var analysis in seg.AnalysesRS)
					{
						IWfiGloss wordGloss = null;
						if (!(analysis is IPunctuationForm))
						{
							string gloss;
							wordGloss = SetDefaultWordGloss(iseg, isegform, out gloss);
						}
						wordGlosses.Add(wordGloss);
						isegform++;
					}
					// create freeform annotations for each of the segments
					// TODO: multiple writing systems.
					ITsString tssComment;
					SetDefaultFreeTranslation(iseg, out tssComment);
					SetDefaultLiteralTranslation(iseg, out tssComment);
					SetDefaultNote(iseg, out tssComment);

					iseg++;
				}
				return wordGlosses;
			}

			/// <summary>
			/// Make up a phony Free Translation for the segment.
			/// </summary>
			public virtual void SetDefaultFreeTranslation(int iSegment, out ITsString tssComment)
			{
				var seg = GetSegment(iSegment);
				ITsString tssSegment = seg.BaselineText;
				string comment = String.Format("{0}.Type({1}).{2}", iSegment, "free", tssSegment.Text);
				seg.FreeTranslation.set_String(m_cache.DefaultAnalWs, comment);
				tssComment = seg.FreeTranslation.AnalysisDefaultWritingSystem;
			}

			/// <summary>
			/// Make up a phony Literal Translation for the segment.
			/// </summary>
			public virtual void SetDefaultLiteralTranslation(int iSegment, out ITsString tssComment)
			{
				var seg = GetSegment(iSegment);
				ITsString tssSegment = seg.BaselineText;
				string comment = String.Format("{0}.Type({1}).{2}", iSegment, "literal", tssSegment.Text);
				seg.LiteralTranslation.set_String(m_cache.DefaultAnalWs, comment);
				tssComment = seg.LiteralTranslation.AnalysisDefaultWritingSystem;
			}

			/// <summary>
			/// Make up a phony Note for the segment.
			/// </summary>
			public virtual void SetDefaultNote(int iSegment, out ITsString tssComment)
			{
				var seg = GetSegment(iSegment);
				ITsString tssSegment = seg.BaselineText;
				string comment = String.Format("{0}.Type({1}).{2}", iSegment, "note", tssSegment.Text);
				var note = seg.Services.GetInstance<INoteFactory>().Create();
				seg.NotesOS.Add(note);
				note.Content.set_String(m_cache.DefaultAnalWs, comment);
				tssComment = note.Content.AnalysisDefaultWritingSystem;
			}
			/// <summary>
			/// Finds an existing wfiAnalysis related to the given cba.InstanceOf.
			/// If none exists, we'll create one.
			/// </summary>
			private IWfiAnalysis FindOrCreateWfiAnalysis(IAnalysis analysisActual)
			{
				IWfiAnalysis actualWfiAnalysis = analysisActual.Analysis;
				if (actualWfiAnalysis == null)
					actualWfiAnalysis = CreateWfiAnalysisForAnalysis(analysisActual);
				return actualWfiAnalysis;
			}

			private IWfiAnalysis CreateWfiAnalysisForAnalysis(IAnalysis actualAnalysis)
			{
				int hvoActualInstanceOf;
				IWfiWordform actualWordform;
				GetRealWordformInfo(actualAnalysis, out hvoActualInstanceOf, out actualWordform);

				// Create a new WfiAnalysis for the wordform.
				IWfiAnalysis actualWfiAnalysis = m_cache.ServiceLocator.GetInstance<IWfiAnalysisFactory>().Create();
				actualWordform.AnalysesOC.Add(actualWfiAnalysis);
				return actualWfiAnalysis;
			}

			/// <summary>
			/// Creates a new analysis with the given MoForms belonging to the wordform currently
			/// at the given position and inserts it into the segment's analysis in place of the wordform.
			/// </summary>
			virtual public IWfiAnalysis BreakIntoMorphs(int iSegment, int iSegForm, ArrayList moForms)
			{
				var actualAnalysis = GetAnalysis(iSegment, iSegForm);
				// Find or create the current analysis of the actual annotation.
				IWfiAnalysis actualWfiAnalysis = CreateWfiAnalysisForAnalysis(actualAnalysis);

				// Setup WfiMorphBundle(s)
				IWfiMorphBundleFactory factory = m_cache.ServiceLocator.GetInstance<IWfiMorphBundleFactory>();
				foreach (object morphForm in moForms)
				{
					IWfiMorphBundle wmb = factory.Create();
					actualWfiAnalysis.MorphBundlesOS.Add(wmb);
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
				SetAnalysis(iSegment, iSegForm, actualWfiAnalysis);
				return actualWfiAnalysis;
			}

			virtual public IWfiMorphBundle SetMorphSense(int iSegment, int iSegForm, int iMorphBundle, ILexSense sense)
			{
				var analysis = GetAnalysis(iSegment, iSegForm);
				// Find or create the current analysis of the actual annotation.
				return SetMorphBundle(analysis, iMorphBundle, sense);
			}

			protected IWfiMorphBundle SetMorphBundle(IAnalysis analysis, int iMorphBundle, ILexSense sense)
			{
				IWfiAnalysis actualWfiAnalysis = analysis.Analysis;
				if (actualWfiAnalysis != null)
				{
					actualWfiAnalysis.MorphBundlesOS[iMorphBundle].SenseRA = sense;
					return actualWfiAnalysis.MorphBundlesOS[iMorphBundle];
				}
				return null;
			}


			virtual internal protected int MergeAdjacentAnnotations(int iSegment, int iSegForm)
			{
				var analysisOccurrence = new AnalysisOccurrence(m_para.SegmentsOS[iSegment], iSegForm);
				analysisOccurrence.MakePhraseWithNextWord();
				NeedReparseParagraph = true;
				return analysisOccurrence.Analysis.Hvo;
			}

			virtual internal protected void BreakPhrase(int iSegment, int iSegForm)
			{
				var analysisOccurrence = new AnalysisOccurrence(m_para.SegmentsOS[iSegment], iSegForm);
				analysisOccurrence.BreakPhrase();
			}

			ITsString GetBaselineText(int iSegment, int iSegForm)
			{
				return m_para.SegmentsOS[iSegment].GetBaselineText(iSegForm);
			}

			private void GetRealWordformInfo(IAnalysis actualAnalysis, out int hvoActualInstanceOf, out IWfiWordform realWordform)
			{
				hvoActualInstanceOf = actualAnalysis.Hvo;
				realWordform = actualAnalysis.Wordform;
			}

			internal IAnalysis GetAnalysis(int iSegment, int iAnalysis)
			{
				var segmentForms = GetSegment(iSegment).AnalysesRS;
				Debug.Assert(iAnalysis >= 0 && iAnalysis < segmentForms.Count);
				return segmentForms[iAnalysis];
			}

			internal int GetSegmentHvo(int iSegment)
			{
				return GetSegment(iSegment).Hvo;
			}

			internal ISegment GetSegment(int iSegment)
			{
				Debug.Assert(m_para != null);
				var segments = m_para.SegmentsOS;
				Debug.Assert(iSegment >= 0 && iSegment < segments.Count);
				return segments[iSegment];
			}
		}
#pragma warning restore 1591

		internal class ParagraphAnnotatorForParagraphBuilder : ParagraphAnnotator
		{
			ParagraphBuilder m_pb = null;

			internal ParagraphAnnotatorForParagraphBuilder(ParagraphBuilder pb) : base(pb.ActualParagraph)
			{
				m_pb = pb;
			}

			override internal IWfiWordform SetAlternateCase(string wordform, int iOccurrenceInParagraph, StringCaseStatus targetState)
			{
				int iSegment = -1;
				int iSegForm = -1;
				m_pb.GetSegmentFormInfo(wordform, iOccurrenceInParagraph, out iSegment, out iSegForm);
				string alternateWordform;
				return SetAlternateCase(iSegment, iSegForm, targetState, out alternateWordform);
			}

			override internal IWfiGloss SetDefaultWordGloss(string wordform, int iOccurrenceInParagraph)
			{
				int iSegment = -1;
				int iSegForm = -1;
				m_pb.GetSegmentFormInfo(wordform, iOccurrenceInParagraph, out iSegment, out iSegForm);
				string gloss;
				return SetDefaultWordGloss(iSegment, iSegForm, out gloss);
			}

			override public IWfiWordform SetAlternateCase(int iSegment, int iSegForm, StringCaseStatus targetState, out string alternateCaseForm)
			{
				IWfiWordform wfAlternateCase = base.SetAlternateCase(iSegment, iSegForm, targetState, out alternateCaseForm);
				m_pb.SetExpectedValuesForAnalysis(m_pb.SegmentFormNode(iSegment, iSegForm), wfAlternateCase.Hvo);
				return wfAlternateCase;
			}

			override public IWfiAnalysis BreakIntoMorphs(int iSegment, int iSegForm, ArrayList moForms)
			{
				IWfiAnalysis wfiAnalysis = base.BreakIntoMorphs(iSegment, iSegForm, moForms);
				m_pb.SetExpectedValuesForAnalysis(m_pb.SegmentFormNode(iSegment, iSegForm), wfiAnalysis.Hvo);
				return wfiAnalysis;
			}

			/// <summary>
			///
			/// </summary>
			/// <param name="iSegFormPrimary">primary segment form index to merge to adjacent segform</param>
			/// <param name="iSegmentPrimary"></param>
			/// <param name="secondaryPathsToJoinWords">list containing Segment/SegmentForm pairs in paragraph to also join.</param>
			/// <param name="secondaryPathsToBreakPhrases">list containint Segment/SegmentForm pairs to break into annotations.</param>
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

			internal void BreakPhrase(int iSegmentPrimary, int iSegFormPrimary,
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
				base.BreakPhrase(iSegmentPrimary, iSegFormPrimary);
			}

			/// <summary>
			/// </summary>
			/// <returns>hvo of new analysis.</returns>
			override internal IWfiGloss SetDefaultWordGloss(int iSegment, int iSegForm, out string gloss)
			{
				return SetDefaultWordGloss(iSegment, iSegForm, null, out gloss);
			}

			/// <summary>
			///
			/// </summary>
			/// <param name="iSegment"></param>
			/// <param name="iSegForm"></param>
			/// <param name="wfiAnalysis">wfi analysis to add gloss to.</param>
			/// <param name="gloss"></param>
			/// <returns></returns>
			override internal IWfiGloss SetDefaultWordGloss(int iSegment, int iSegForm, IWfiAnalysis wfiAnalysis, out string gloss)
			{
				IWfiGloss wfiGloss = base.SetDefaultWordGloss(iSegment, iSegForm, wfiAnalysis, out gloss);
				m_pb.SetExpectedValuesForAnalysis(m_pb.SegmentFormNode(iSegment, iSegForm), wfiGloss.Hvo);
				return wfiGloss;
			}

			/// <summary>
			/// In the ID attribute of the XmlNode we store the ID of the analysis we expect for that occurrence.
			/// </summary>
			private int GetIdOfExpectedAnalysis(int iSegment, int iSegForm)
			{
				XmlNode cbaWordformNode = m_pb.SegmentFormNode(iSegment, iSegForm);
				return XmlUtils.GetMandatoryIntegerAttributeValue(cbaWordformNode, "id");
			}

			/// <summary>
			/// Walk through the paragraph and create word glosses, and segment annotations.
			/// </summary>
			/// <returns>list of wordglosses, including 0's for nonwordforms</returns>
			internal protected override IList<IWfiGloss> SetupDefaultWordGlosses()
			{
				IList<IWfiGloss> wordGlosses = new List<IWfiGloss>();
				int iseg = 0;
				foreach (XmlNode segNode in m_pb.SegmentNodes())
				{
					int isegform = 0;
					foreach (XmlNode segFormNode in ParagraphBuilder.SegmentFormNodes(segNode))
					{
						IWfiGloss wordGloss = null;
						string annTypeGuid = ParagraphBuilder.GetAnnotationTypeGuid(segFormNode);
						if (annTypeGuid == ParagraphBuilder.WficGuid)
						{
							string gloss;
							wordGloss = SetDefaultWordGloss(iseg, isegform, out gloss);
						}
						if (annTypeGuid == ParagraphBuilder.PunctGuid)
						{
							m_pb.ExportCbaNodeToReal(iseg, isegform);
						}
						wordGlosses.Add(wordGloss);
						isegform++;
					}
					// create freeform annotations for each of the segments
					// TODO: multiple writing systems.
					ITsString tssComment;
					SetDefaultFreeTranslation(iseg, out tssComment);
					SetDefaultLiteralTranslation(iseg, out tssComment);
					SetDefaultNote(iseg, out tssComment);

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
			IFdoOwningSequence<IStPara> m_owner = null;
			IStTxtPara m_para = null;
			XmlNode m_paraDefn = null;
			bool m_fNeedToRebuildParagraphContentFromAnnotations = false;
			Dictionary<string, int> m_expectedWordformsAndOccurrences;
			//bool m_fNeedToRebuildParagraphContentFromStrings = false;

			internal ParagraphBuilder(FdoCache cache, IFdoOwningSequence<IStPara> owner)
			{
				m_owner = owner;
				m_cache = cache;
			}

			internal ParagraphBuilder(FdoCache cache, IFdoOwningSequence<IStPara> owner, XmlNode paraDefn)
				: this(cache, owner)
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

			internal Dictionary<string, int> ExpectedWordformsAndOccurrences
			{
				get
				{
					if (m_expectedWordformsAndOccurrences == null)
						GenerateParaContentFromAnnotations();
					return m_expectedWordformsAndOccurrences;
				}
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
			/// <summary>
			/// Copies out the anaysis info in the paragraph definition cba node into the appropriate position in the real segment.
			/// Will replace the current one at that position, or may be used to add to the segment, by passing
			/// an index one too large.
			/// Returns the analysis.
			/// </summary>
			internal IAnalysis ExportCbaNodeToReal(int iSegment, int iSegForm)
			{
				int hvoDesiredTarget = GetExpectedAnalysis(iSegment, iSegForm);
				var seg = m_para.SegmentsOS[iSegment];
				IAnalysis analysis;
				if (hvoDesiredTarget == -1)
				{
					string word = StringValue(iSegment, iSegForm);
					int wsWord = GetWsFromStringNode(StringValueNode(iSegment, iSegForm));
					var wfFactory =
						m_cache.ServiceLocator.GetInstance<IWfiWordformFactory>();
					analysis = wfFactory.Create(TsStringUtils.MakeTss(word, wsWord));
				}
				else
				{
					analysis = (IAnalysis) seg.Services.GetObject(hvoDesiredTarget);
				}
				if (iSegForm == seg.AnalysesRS.Count)
					seg.AnalysesRS.Add(analysis);
				else
					seg.AnalysesRS[iSegForm] = analysis;
				return analysis;
			}

			internal int GetExpectedAnalysis(int iSegment, int iSegForm)
			{
				XmlNode fileItem = SegmentFormNode(iSegment, iSegForm);
				return int.Parse(fileItem.Attributes["id"].Value);
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

			[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
				Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
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
				if (m_para.Contents == null)
				{
				   // make sure it has an empty content
					m_para.Contents = TsStringUtils.MakeTss("", m_cache.DefaultVernWs);
				}
				return m_para;
			}

			[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
				Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
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
							bldr.AppendRun(run.InnerText, MakeTextProps(ws));
						}
						SetContents(bldr.StringBuilder.GetString(), false);
						return m_para.Contents;
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
				paraTarget.Contents = paraSrc.Contents;
			}

			/// <summary>
			/// (after setting paragraph contents, we'll reparse the text, creating dummy wordforms.)
			/// </summary>
			/// <returns></returns>
			internal ITsString RebuildParagraphContentFromAnnotations()
			{
				return RebuildParagraphContentFromAnnotations(false);
			}

			/// <summary>
			///
			/// </summary>
			/// <param name="fResetConcordance"></param>
			/// <returns></returns>
			internal ITsString RebuildParagraphContentFromAnnotations(bool fResetConcordance)
			{
				try
				{
					ITsString content = GenerateParaContentFromAnnotations();
					if (content == null)
						return null;

					// Set the content of text for the paragraph.
					this.SetContents(content, fResetConcordance);
					return m_para.Contents;
				}
				finally
				{
					m_fNeedToRebuildParagraphContentFromAnnotations = false;
				}
			}

			/// <summary>
			/// Currently, this ensures that the real analysis is set for all segments and wordforms up to the
			/// specified one. We can't have an isolated item any more, so we create earlier ones
			/// as necessary, also.
			/// </summary>
			/// <param name="iSegment"></param>
			/// <param name="iSegForm"></param>
			/// <returns></returns>
			internal IWfiWordform SetDefaultWord(int iSegment, int iSegForm)
			{
				// Make sure we have enough segments
				if (iSegment >= m_para.SegmentsOS.Count)
				{
					for (int i = m_para.SegmentsOS.Count; i < iSegment; iSegForm++)
					{
						var newSeg = m_para.Services.GetInstance<ISegmentFactory>().Create();
						m_para.SegmentsOS.Add(newSeg);
					}
				}

				// Make sure that any earlier wordforms for this segment exist. Otherwise, we can't set the
				// one at the specified index. Note that this is a recursive call; it will terminate because
				// we always make the recursive call with a smaller value of iSegForm. In fact, it will only
				// recurse once, because we make sure to create the first one needed first.
				var seg = m_para.SegmentsOS[iSegment];
				for (int i = seg.AnalysesRS.Count; i < iSegForm; i++)
					SetDefaultWord(iSegment, i);

				// Now make the one we actually want, even if it already had an item in the Analyses list.
				string stringValue = StringValue(iSegment, iSegForm);
				int ws = GetWsFromStringNode(StringValueNode(iSegment, iSegForm));
				IWfiWordform actualWordform = WfiWordformServices.FindOrCreateWordform(m_cache, TsStringUtils.MakeTss(stringValue, ws));
				XmlNode fileItem = SegmentFormNode(iSegment, iSegForm);
				fileItem.Attributes["id"].Value = actualWordform.Hvo.ToString();
				ExportCbaNodeToReal(iSegment, iSegForm);
				return actualWordform;
			}

			/// <summary>
			/// Sync the expected occurrences with the database.
			/// </summary>
			internal void ResyncExpectedAnnotationIds()
			{
				GenerateParaContentFromAnnotations();
			}

			internal static string WficGuid = "eb92e50f-ba96-4d1d-b632-057b5c274132";
			internal static string PunctGuid = "cfecb1fe-037a-452d-a35b-59e06d15f4df";
			/// <summary>
			/// Create the content of a translation property of a segment so as to be consistent with the
			/// analysis glosses.
			/// </summary>
			/// <returns></returns>
			[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
				Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
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
				ITsStrBldr contentsBldr = TsStrBldrClass.Create();
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
						if (prevAnnType != null && prevAnnType == guid && prevAnnType == WficGuid)
						{
							// add standard form separator between common annotation types.
							leadingWhitespace += " ";
						}
						int ichMinForm = ichLimSeg + leadingWhitespace.Length;
						int ichLimForm = ichMinForm + strForm.Length;
						// if the 'id' is valid, then use it.
						// Create a dummy annotation for the appropriate type.
						if (guid == WficGuid)
						{
							// If this is the first time we've built the contents of this paragraph,
							// the id attribute will be empty, and should be set to -1 to indicate
							// we just expect a wordform. Otherwise, it has previously been set to some
							// specific expected thing, so leave it alone.
							if (string.IsNullOrEmpty(form.Attributes[kstAnalysisAttr].Value))
								SetExpectedValuesForAnalysis(form, -1);
							// update our expected occurences of this wordform.
							UpdateExpectedWordformsAndOccurrences(strForm, ws);
						}
						else
						{
							if (guid == PunctGuid)
							{
								SetExpectedValuesForAnalysis(form, -1); // -1 indicates a plain punctform.
							}
							else
							{
								Debug.Fail("AnnotationType " + guid + " not supported in SegmentForms.");
							}
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
					SetExpectedValuesForSegment(segment, ichMinSeg);
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
					IWritingSystem wsObj = (IWritingSystem)m_cache.WritingSystemFactory.get_Engine(wsAbbr);
					ws = wsObj.Handle;
					Debug.Assert(ws != 0, "Don't recognize ws (" + wsAbbr + ") for StringValue");
					// add it to the vernacular writing system list, if it's not already there.
					if (!m_cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems.Contains(wsObj))
						m_cache.ServiceLocator.WritingSystems.AddToCurrentVernacularWritingSystems(wsObj);
				}
				if (ws == 0)
					ws = m_cache.DefaultVernWs;
				return ws;
			}

			private const string kstAnalysisAttr = "id";
			/// <summary>
			/// Set the expected values that we computer or modify rather than reading from the file, for an
			/// XmlNode that represents an analysis.
			/// </summary>
			internal void SetExpectedValuesForAnalysis(XmlNode annotationDefn, int hvoInstanceOf)
			{
				annotationDefn.Attributes[kstAnalysisAttr].Value = hvoInstanceOf.ToString();
			}

			/// <summary>
			/// Set the expected values that we computer or modify rather than reading from the file, for an
			/// XmlNode that represents an analysis.
			/// </summary>
			internal void SetExpectedValuesForSegment(XmlNode annotationDefn, int ichMin)
			{
				if (annotationDefn.Attributes["beginOffset"] == null)
					XmlUtils.AppendAttribute(annotationDefn, "beginOffset", ichMin.ToString());
				else
					annotationDefn.Attributes["beginOffset"].Value = ichMin.ToString();
			}

			public IStTxtPara AppendParagraph()
			{
				return CreateParagraph(m_owner.Count);
			}

			public IStTxtPara CreateParagraph(int iInsertNewParaAt)
			{
				Debug.Assert(m_owner != null);
				IStTxtPara para = m_cache.ServiceLocator.GetInstance<IStTxtParaFactory>().Create();
				m_owner.Insert(iInsertNewParaAt, para);
				return para;
			}

			void SetContents(ITsString tssContents, bool fResetConcordance)
			{
				Debug.Assert(m_para != null);
				if (m_para.Contents != null && tssContents.Equals(m_para.Contents))
					return;
				m_para.Contents = tssContents;
				//ParseParagraph(fResetConcordance, fCreateRealWordforms);
			}

			bool HaveParsed { get; set; }

			internal void ParseParagraph()
			{
				ParseParagraph(false, false);
			}

			internal void ParseParagraph(bool fResetConcordance)
			{
				// Reparse the paragraph to recompute annotations.
				ParseParagraph(fResetConcordance, true);
			}

			internal void ParseParagraph(bool fResetConcordance, bool fBuildConcordance)
			{
				ParagraphParser.ParseParagraph(m_para, fBuildConcordance, fResetConcordance);
				HaveParsed = true;
			}

			[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
				Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
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

			/// <summary>
			/// In the XML representation of expected data, each Segment has a SegmentForms37 property containing
			/// a sequence of CmBaseAnnotations. Given the parent node, answer the CmBaseAnnotation nodes.
			/// </summary>
			/// <param name="segmentNode"></param>
			/// <returns></returns>
			static XmlNodeList SegmentFormNodeList(XmlNode segmentNode)
			{
				XmlNodeList cbaSegFormNodes = segmentNode.SelectNodes("SegmentForms37/CmBaseAnnotation");
				return cbaSegFormNodes;
			}

			[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
				Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
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

			/// <summary>
			/// Get a list of the XmlNodes (CmBaseAnnotations in the Segments16 property) of the input XmlNode,
			/// which represents an StTxtPara in our test data.
			/// </summary>
			[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
				Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
			static internal List<XmlNode> SegmentNodes(XmlNode paraDefn)
			{
				return NodeListToNodes(SegmentNodeList(paraDefn));
			}

			[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
				Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
			internal List<XmlNode> SegmentNodes()
			{
				return NodeListToNodes(SegmentNodeList());
			}

			[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
				Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
			static internal List<XmlNode> SegmentFormNodes(XmlNode segment)
			{
				return NodeListToNodes(SegmentFormNodeList(segment));
			}

			[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
				Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
			internal XmlNode GetSegmentFormInfo(string wordform, int iOccurrenceInParagraph, out int iSegment, out int iSegForm)
			{
				XmlNodeList cbaSegFormNodes = m_paraDefn.SelectNodes("./Segments16/CmBaseAnnotation/SegmentForms37/CmBaseAnnotation[StringValue37='" + wordform + "']");
				Debug.Assert(cbaSegFormNodes != null);
				Debug.Assert(iOccurrenceInParagraph >= 0 && iOccurrenceInParagraph < cbaSegFormNodes.Count);
				XmlNode cbaSegForm = cbaSegFormNodes[iOccurrenceInParagraph];
				iSegForm = XmlUtils.GetIndexAmongSiblings(cbaSegForm);
				iSegment = XmlUtils.GetIndexAmongSiblings(cbaSegForm.ParentNode.ParentNode);
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
				Debug.Assert(annTypeGuid == WficGuid ||
							 annTypeGuid == PunctGuid,
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

			[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
				Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
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
				InsertSegmentForm(iSegment, iSegForm, PunctGuid, stringValue);

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

			[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
				Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
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
						xaWs.Value = m_cache.ServiceLocator.WritingSystemManager.GetStrFromWs(newWs);
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
					this.InsertSegmentForm(iSegment, iSegForm + 1, WficGuid, word);
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

			/// <summary>
			/// Given an XmlNode from our test data representing an StTxtPara, get the (obsolete structure) CmBaseAnnotation
			/// nodes representing its segments.
			/// </summary>
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

			/// <summary>
			/// Given an XmlNode representing a CmBaseAnnotation, find the id of the associated analysis object.
			/// </summary>
			/// <param name="cbaNode"></param>
			/// <returns></returns>
			static internal int GetAnalysisId(XmlNode cbaNode)
			{
				return XmlUtils.GetMandatoryIntegerAttributeValue(cbaNode, "id");
			}
		}

		/// <summary>
		/// This can be used to build a Text based upon an xml specification.
		/// Note: the xml specification will be modified to save the expected state of the text.
		/// </summary>
		protected internal class TextBuilder
		{
			FdoCache m_cache = null;
			//IFdoOwningCollection<FDO.IText> m_owner = null;
			FDO.IText m_text = null;
			XmlNode m_textDefn = null;

			internal TextBuilder(TextBuilder tbToClone)
				: this(tbToClone.m_cache)
			{
				m_text = tbToClone.m_text;
				this.SelectedNode = ParagraphBuilder.Snapshot(tbToClone.SelectedNode);
			}

			internal TextBuilder(FdoCache cache)
			{
				//m_owner = owner;
				m_cache = cache;
			}

			[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
				Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
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
						ws = m_cache.ServiceLocator.WritingSystemManager.GetWsFromStr(wsAbbr);
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
						ParagraphBuilder pb = new ParagraphBuilder(m_cache, m_text.ContentsOA.ParagraphsOS);
						IStTxtPara realPara = pb.BuildParagraphContent(paraDef);
					}
				}

				return m_text;
			}

			/// <summary>
			///
			/// </summary>
			/// <returns></returns>
			public void GenerateAnnotationIdsFromDefn()
			{
				foreach (IStTxtPara para in m_text.ContentsOA.ParagraphsOS)
				{
					ParagraphBuilder pb = GetParagraphBuilder(para);
					pb.GenerateParaContentFromAnnotations();
				}
			}

			/// <summary>
			/// TODO: Finish implementing this.
			/// </summary>
			/// <returns></returns>
			public List<int> ExportRealSegmentAnnotationsFromDefn()
			{
				List<int> realSegments = new List<int>();
				foreach (IStTxtPara para in m_text.ContentsOA.ParagraphsOS)
				{
					ParagraphBuilder pb = GetParagraphBuilder(para);
					ParagraphAnnotatorForParagraphBuilder papb = new ParagraphAnnotatorForParagraphBuilder(pb);
					int iseg = 0;
					foreach (XmlNode segNode in pb.SegmentNodes())
					{
						int hvoSeg = papb.GetSegmentHvo(iseg);
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
				Debug.Assert(m_cache != null);
				FDO.IText newText = m_cache.ServiceLocator.GetInstance<ITextFactory>().Create();
				//Debug.Assert(m_owner != null);
				//m_owner.Add(newText);
				return newText;
			}


			IStText CreateContents(FDO.IText text)
			{
				Debug.Assert(text != null);
				IStText body1 = m_cache.ServiceLocator.GetInstance<IStTextFactory>().Create();
				text.ContentsOA = body1;
				return body1;
			}

			/// <summary>
			///
			/// </summary>
			/// <returns></returns>
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

			/// <summary>
			///
			/// </summary>
			/// <param name="paragraphDefnToInsertAfter"></param>
			/// <returns></returns>
			public IStTxtPara InsertNewParagraphAfter(XmlNode paragraphDefnToInsertAfter)
			{
				ParagraphBuilder pb = new ParagraphBuilder(m_cache, m_text.ContentsOA.ParagraphsOS);
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
				//m_owner.Remove(m_text);
				HvoActualStText = 0;
			}

			internal void DeleteParagraphDefn(IStTxtPara para)
			{
				ParagraphBuilder pb = GetParagraphBuilder(para);
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
			/// <param name="startingNodeToMoveToNewPara"></param>
			/// <returns>the XmlNode corresponding to the new paragraph defn</returns>
			internal XmlNode InsertParagraphBreak(IStTxtPara para, XmlNode startingNodeToMoveToNewPara)
			{
				// IStTxtPara/Segments/CmBaseAnnotation[]
				ParagraphBuilder pb = GetParagraphBuilder(para);
				// make a new paragraph node after the current one
				if (pb.ParagraphDefinition != null)
				{
					XmlNode newParaDefn = InsertNewParagraphDefn(pb.ParagraphDefinition);
					ParagraphBuilder pbNew = new ParagraphBuilder(m_cache, m_text.ContentsOA.ParagraphsOS, newParaDefn);
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

			[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
				Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
			internal void DeleteParagraphBreak(XmlNode paraNodeToDelBreak)
			{
				int iParaToDeleteBreak = XmlUtils.GetIndexAmongSiblings(paraNodeToDelBreak);
				XmlNodeList paragraphs = paraNodeToDelBreak.ParentNode.SelectNodes("StTxtPara");
				XmlNode nextParaNode = paragraphs[iParaToDeleteBreak + 1];
				// IStTxtPara/Segments/CmBaseAnnotation[]
				MoveSiblingNodes(nextParaNode.FirstChild.FirstChild, paraNodeToDelBreak.FirstChild, null);
				DeleteParagraphDefn(nextParaNode);
			}
			internal void DeleteParagraphBreak(IStTxtPara para)
			{
				ParagraphBuilder pb1 = GetParagraphBuilder(para);
				DeleteParagraphBreak(pb1.ParagraphDefinition);
			}

			internal int HvoActualStText
			{
				get
				{
					if (m_text != null)
						return m_text.ContentsOA.Hvo;
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
						IStText stText = m_cache.ServiceLocator.GetInstance<IStTextRepository>().GetObject(value);
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
					ParagraphBuilder pb = this.GetParagraphBuilder(para);
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

			[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
				Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
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

			internal IText ActualText
			{
				get
				{
					if (m_text == null)
						return null;
					return m_text as IText;
				}
			}

			internal IStText ActualStText
			{
				get
				{
					if (m_text == null || m_text.ContentsOA.Hvo == 0)
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
			/// <param name="para"></param>
			/// <returns></returns>
			internal ParagraphBuilder GetParagraphBuilder(IStTxtPara para)
			{
				int iPara = para.IndexInOwner;
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
				return new ParagraphBuilder(m_cache, m_text.ContentsOA.ParagraphsOS, paraDefn);
			}

			void SetName(string name, int ws)
			{
				m_text.Name.set_String(ws, name);
			}

		}

	}

	/// <summary>
	/// </summary>
	[TestFixture]
	public class ParagraphParserTests: InterlinearTestBase
	{
		FDO.IText m_text1 = null;
		private XmlNode m_testFixtureTextsDefn = null;
		XmlDocument m_textsDefn = null;
		private IWritingSystem m_wsEn = null;
		private IWritingSystem m_wsXkal = null;

		/// <summary>
		///
		/// </summary>
		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			m_textsDefn = new XmlDocument();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor,
				DoSetupFixture);
		}

		/// <summary>
		/// non-undoable task
		/// </summary>
		private void DoSetupFixture()
		{
			// Setup default analysis ws
			m_wsEn = Cache.ServiceLocator.WritingSystemManager.Get("en");

			// setup default vernacular ws.
			m_wsXkal = Cache.ServiceLocator.WritingSystemManager.Set("qaa-x-kal");
			m_wsXkal.DefaultFontName = "Times New Roman";
			Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem = m_wsXkal;

			m_text1 = LoadTestText(
							Path.Combine("FDO",
							 Path.Combine("FDOTests",
							  Path.Combine("TestData", "ParagraphParserTestTexts.xml"))),
								1, m_textsDefn);
			// capture text defn state.
			m_testFixtureTextsDefn = m_textsDefn;
		}

		/// <summary>
		///
		/// </summary>
		[TestFixtureTearDown]
		public override void FixtureTeardown()
		{
			m_textsDefn = null;
			m_text1 = null;

			base.FixtureTeardown();
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
			IWritingSystem wsObj = Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem;
			var validChars = ValidCharacters.Load(wsObj.ValidChars,
				wsObj.DisplayLabel, null, null, FwDirectoryFinder.LegacyWordformingCharOverridesFile);
			var fChangedSomething = false;
			if (!validChars.IsWordForming('-'))
			{
				validChars.AddCharacter("-");
				validChars.MoveBetweenWordFormingAndOther(new List<string>(new[] { "-" }), true);
				fChangedSomething = true;
			}
			if (!validChars.IsWordForming('\''))
			{
				validChars.AddCharacter("'");
				validChars.MoveBetweenWordFormingAndOther(new List<string>(new[] { "'" }), true);
				fChangedSomething = true;
			}
			if (!fChangedSomething)
				return;
			wsObj.ValidChars = validChars.XmlString;
		}

		private void RestoreTextDefn()
		{
			m_textsDefn = ParagraphBuilder.Snapshot(m_testFixtureTextsDefn) as XmlDocument;
		}


		/// <summary>
		/// Restore our TestFixture data for each test use.
		/// </summary>
		[SetUp]
		public override void TestSetup()
		{
			base.TestSetup();

			RestoreTextDefn();
		}

		/// <summary>
		/// Test the method that gets the reference of an AnalysisOccurrence
		/// Enhance: It would be nice to test references for more than one paragraph.
		/// </summary>
		[Test]
		public void AnalysisReference()
		{
			// Make a text like "my green mat".
			var sttext = MakeText("My Green mat");
			var seg1 = MakeSegment(sttext, "pus yalola nihimbilira.");
			var analysis1 = MakeWordformAnalysis(seg1, "pus");
			Assert.That(analysis1.Reference.Text, Is.EqualTo("My Green 1.1"));
			//// Now try for one in the second segment.
			var analysis2 = MakeWordformAnalysis(seg1, "yalola");
			var analysis3 = MakeWordformAnalysis(seg1, "nihimbilira");
			var seg2 = MakeSegment(sttext, "hesyla nihimbilira.");
			var analysis4 = MakeWordformAnalysis(seg2, "hesyla");
			var analysis5 = MakeWordformAnalysis(seg2, "nihimbilira");
			Assert.That(analysis5.Reference.Text, Is.EqualTo("My Green 1.2"));

			//// Set an empty text name and check.
			var text = (IText) sttext.Owner;
			text.Name.AnalysisDefaultWritingSystem = Cache.TsStrFactory.EmptyString(Cache.DefaultAnalWs);
			Assert.That(analysis1.Reference.Text, Is.EqualTo("1.1"));

			//// Try with a name less than 5 chars.
			text.Name.AnalysisDefaultWritingSystem = Cache.TsStrFactory.MakeString("abc", Cache.DefaultAnalWs);
			Assert.That(analysis1.Reference.Text, Is.EqualTo("abc 1.1"));

			// It prefers to use the abbreviation.
			text.Abbreviation.AnalysisDefaultWritingSystem = Cache.TsStrFactory.MakeString("mg", Cache.DefaultAnalWs);
			Assert.That(analysis1.Reference.Text, Is.EqualTo("mg 1.1"));
		}

		private AnalysisOccurrence MakeWordformAnalysis(ISegment seg, string form)
		{
			var wf = WfiWordformServices.FindOrCreateWordform(Cache, form,
				Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem);
			seg.AnalysesRS.Add(wf);
			return new AnalysisOccurrence(seg, seg.AnalysesRS.Count - 1);
		}

		/// <summary>
		/// Add a segment of text to the paragraph and return the resulting segment.
		/// Note that this depends on the code that automatically reparses the paragraph,
		/// so the strings added must really produce segments.
		/// </summary>
		private ISegment MakeSegment(IStText text,string contents)
		{
			var para = (IStTxtPara) text.ParagraphsOS[0];
			int length = para.Contents.Length;
			int start = 0;
			if (length == 0)
				para.Contents = Cache.TsStrFactory.MakeString(contents, Cache.DefaultVernWs);
			else
			{
				var bldr = para.Contents.GetBldr();
				bldr.Replace(length, length, " " + contents, null);
				para.Contents = bldr.GetString();
				start = length + 1;
			}
			var seg = para.SegmentsOS[para.SegmentsOS.Count - 1];
			return seg;
		}

		private IStText MakeText(string title)
		{
			var text = Cache.ServiceLocator.GetInstance<ITextFactory>().Create();
			//Cache.LangProject.TextsOC.Add(text);
			var result = Cache.ServiceLocator.GetInstance<IStTextFactory>().Create();
			text.ContentsOA = result;
			text.Name.AnalysisDefaultWritingSystem = Cache.TsStrFactory.MakeString(title, Cache.DefaultAnalWs);
			var para = Cache.ServiceLocator.GetInstance<IStTxtParaFactory>().Create();
			result.ParagraphsOS.Add(para);
			return result;
		}

		enum Text1ParaIndex {
			/// <summary>
			/// xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.
			/// </summary>
			SimpleSegmentPara = 0,
			/// <summary> ""; don't put this first as WS is not well established. </summary>
			EmptyParagraph,
			/// <summary>
			/// <code>xxxnihimbiligu  xxxyaloni, xxxkasani &amp; xxxnihimbilibi... "xxxnihimbilira (xxxpus) xxxkasani."! ! ! xxxpus</code>
			/// </summary>
			ComplexPunctuations,
			/// <summary>
			/// xxxnihimbili'gu xxxyaloni. xxxkasani1xxxpus xxxnihimbilibi 123. xxxnihimbilira xxxpus-kola xxxkasani.
			/// </summary>
			ComplexWordforms,
			/// <summary>
			/// Xxxpus xxxyalola xxxnihimbilira. Xxxnihimbilira xxxpus Xxxyalola. Xxxhesyla XXXNIHIMBILIRA.
			/// </summary>
			MixedCases,
			/// <summary>
			/// xxxpus xxes xxxnihimbilira. xxfr xxen xxxnihimbilira xxxpus xxde. xxkal xxkal xxxxhesyla xxxxhesyla.
			/// </summary>
			MultipleWritingSystems,
			/// <summary>
			/// xxxpus xxxyalola xxxnihimbilira. xxxpus xxxyalola xxxhesyla xxxnihimbilira. xxxpus xxxyalola xxxnihimbilira.
			/// </summary>
			PhraseWordforms};
		/// <summary>
		/// The actual paragraphs are built from ParagraphParserTestTexts.xml. ParagraphContents is simply for auditing.
		/// </summary>
		string[] ParagraphContents = new string[] {
													  // Paragraph 0 - simple segments
													  "xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.",
													  // Paragraph 1 - empty paragraph
													  "",
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
			IWritingSystemManager wsManager = Cache.ServiceLocator.WritingSystemManager;
			int wsXkal = wsManager.GetWsFromStr("xkal");
			int wsEn = wsManager.GetWsFromStr("en");
			int wsFr = wsManager.GetWsFromStr("fr");
			int wsEs = wsManager.GetWsFromStr("es");
			int wsDe = wsManager.GetWsFromStr("de");

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
			Assert.IsNotNull(m_text1);
			Assert.AreEqual(m_text1.Name.VernacularDefaultWritingSystem.Text, "Test Text1 for ParagraphParser");
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
		/// Test the annotations for empty paragraph.
		/// </summary>
		[Test]
		public void NoAnalyses_NoEdits_EmptyParagraph()
		{
			ParagraphBuilder pb = new ParagraphBuilder(m_textsDefn, m_text1, (int)Text1ParaIndex.EmptyParagraph);
			ParagraphAnnotatorForParagraphBuilder tapb = new ParagraphAnnotatorForParagraphBuilder(pb);
			pb.ParseParagraph();
			tapb.ValidateAnnotations();
		}

		/// <summary>
		/// Test the annotations for simple segment paragraph.
		/// </summary>
		[Test]
		public void NoAnalyses_NoEdits_SimpleSegmentParagraph()
		{
			ParagraphBuilder pb = new ParagraphBuilder(m_textsDefn, m_text1, (int)Text1ParaIndex.SimpleSegmentPara);
			ParagraphAnnotatorForParagraphBuilder tapb = new ParagraphAnnotatorForParagraphBuilder(pb);
			pb.ParseParagraph();
			tapb.ValidateAnnotations();
		}

		/// <summary>
		/// Test the annotations for complex punctuation paragraph.
		/// </summary>
		[Test]
		public void NoAnalyses_NoEdits_ComplexPunctuationParagraph()
		{
			ParagraphBuilder pb = new ParagraphBuilder(m_textsDefn, m_text1, (int)Text1ParaIndex.ComplexPunctuations);
			ParagraphAnnotatorForParagraphBuilder tapb = new ParagraphAnnotatorForParagraphBuilder(pb);
			pb.ParseParagraph();
			tapb.ValidateAnnotations();
		}

		/// <summary>
		/// Test the annotations for complex wordform paragraph.
		/// </summary>
		[Test]
		public void NoAnalyses_NoEdits_ComplexWordformsParagraph()
		{
			SetupOldWordformingOverrides();

			ParagraphBuilder pb = new ParagraphBuilder(m_textsDefn, m_text1, (int)Text1ParaIndex.ComplexWordforms);
			ParagraphAnnotatorForParagraphBuilder tapb = new ParagraphAnnotatorForParagraphBuilder(pb);
			pb.ParseParagraph();
			tapb.ValidateAnnotations();
		}

		/// <summary>
		/// Test the annotations for mixed case wordform paragraph.
		/// </summary>
		[Test]
		public void NoAnalyses_NoEdits_MixedCaseWordformsParagraph()
		{
			ParagraphBuilder pb = new ParagraphBuilder(m_textsDefn, m_text1, (int)Text1ParaIndex.MixedCases);
			ParagraphAnnotatorForParagraphBuilder tapb = new ParagraphAnnotatorForParagraphBuilder(pb);
			pb.ParseParagraph();
			tapb.ValidateAnnotations();
		}

		/// <summary>
		/// Test the annotations for mixed case wordform paragraph.
		/// </summary>
		[Test]
		public void NoAnalyses_NoEdits_MultipleWritingSystemsParagraph()
		{
			// For now, just make sure we can build the text and parse it.
			ParagraphBuilder pb = new ParagraphBuilder(m_textsDefn, m_text1, (int)Text1ParaIndex.MultipleWritingSystems);
			pb.ParseParagraph();
		}

		/// <summary>
		/// 0		 1		 2		 3		 4		 5
		/// 012345678901234567890123456789012345678901234567890123456789
		/// xxxpus xxxyalola xxxnihimbilira. xxxpus xxxyalola xxxhesyla xxxnihimbilira. xxxpus xxxyalola xxxnihimbilira.
		/// xxxpus xxxyalola xxxnihimbilira. [xxxpus xxxyalola] xxxhesyla xxxnihimbilira. xxxpus xxxyalola xxxnihimbilira.
		/// </summary>
		[Test]
		public void NoAnalyses_NoEdits_PhraseWordforms()
		{
			// 1. Setup Tests with a basic phrase
			IList<int[]> secondaryPathsToJoinWords = new List<int[]>();
			IList<int[]> secondaryPathsToBreakPhrases = new List<int[]>();

			ParagraphBuilder pb = new ParagraphBuilder(m_textsDefn, m_text1, (int) Text1ParaIndex.PhraseWordforms);

			// first do a basic phrase (without secondary phrases (guesses))
			// xxxpus xxxyalola xxxnihimbilira. [xxxpus xxxyalola] xxxhesyla xxxnihimbilira. xxxpus xxxyalola xxxnihimbilira
			pb.MergeAdjacentAnnotations(1, 0);
			// generate mock ids
			pb.RebuildParagraphContentFromAnnotations();
			// now produce a guess to establish the phrase annotation.
			var tapb = new ParagraphAnnotatorForParagraphBuilder(pb);
			pb.ParseParagraph();
			pb.ActualParagraph.SegmentsOS[1].AnalysesRS.RemoveAt(0); // delete "xxxpus"
			// now replace "xxxyalola" with the new phrase form "xxxpus xxxyalola"
			IAnalysis beforeParse_phrase1_0 = pb.ExportCbaNodeToReal(1, 0);
			//string gloss;
			//IWfiGloss wg_phrase1_0 = tapb.SetDefaultWordGloss(1, 0, out gloss);
			// NOTE: Precondition checks to make sure we set up the annotation properly

			// The real test: now parse and verify that we maintained the expected result for the phrase annotation.
			pb.ParseParagraph();
			var afterParse_actualWordform = tapb.GetAnalysis(1, 0);
			Assert.AreEqual(beforeParse_phrase1_0, afterParse_actualWordform, "word mismatch");
			// verify the rest.
			tapb.ValidateAnnotations();
		}

		/// <summary>
		/// Test the annotations for mixed case wordform paragraph.
		/// </summary>
		[Test]
		public void NoAnalyses_NoEdits_MultipleWritingSystemsParagraph_LT5379()
		{
			var pb = new ParagraphBuilder(m_textsDefn, m_text1, (int)Text1ParaIndex.MultipleWritingSystems);
			var tapb = new ParagraphAnnotatorForParagraphBuilder(pb);
			pb.ParseParagraph();
			tapb.ValidateAnnotations();
		}

		/// <summary>
		/// Change the writing system of a word in the text after parsing it the first time.
		/// xxxpus xxes xxxnihimbilira. xxfr xxen xxxnihimbilira xxxpus xxde. xxkal xxkal xxxxhesyla xxxxhesyla.
		/// </summary>
		[Test]
		public void NoAnalyses_SimpleEdits_MultipleWritingSystemsParagraph()
		{
			ParagraphBuilder pb = new ParagraphBuilder(m_textsDefn, m_text1, (int)Text1ParaIndex.MultipleWritingSystems);
			// verify that our wfics point to wordforms in the expected wss.
			pb.ParseParagraph();
			ParagraphAnnotatorForParagraphBuilder tapb = new ParagraphAnnotatorForParagraphBuilder(pb);
			FdoValidator.ValidateCbaWordToBaselineWord(tapb, 0, 0);
			//FdoValidator.ValidateCbaWordToBaselineWord(tapb, 0, 1); // currently considered punctuation.
			//FdoValidator.ValidateCbaWordToBaselineWord(tapb, 1, 0); // french word considered punctuation.
			//FdoValidator.ValidateCbaWordToBaselineWord(tapb, 1, 1); // eng word considered punctuation
			// FdoValidator.ValidateCbaWordToBaselineWord(tapb, 1, 4); // german word considered punctuation.
			// validate the rest
			tapb.ValidateAnnotations();

			// xxxpus xxes xxxnihimbilira. xxfr xxen xxxnihimbilira xxxpus xxde. xxkal xxkal xxxxhesyla xxxxhesyla.
			//	xxkal: German (de)		-- occurrence 0
			//	xxkal: Kalaba (xkal)	-- occurrence 1
			Dictionary<string, int> expectedOccurrences = pb.ExpectedWordformsAndOccurrences;
			CheckExpectedWordformsAndOccurrences(pb.ActualParagraph, expectedOccurrences);
			// replace the german occurrence of "xxkal" with a xkal version.
			int wsDe;
			FdoValidator.GetTssStringValue(tapb, 2, 0, out wsDe);
			int wsVernDef = Cache.DefaultVernWs;
			Assert.AreNotEqual(wsVernDef, wsDe, "Precondition: did not expect to have a default vern ws.");
			pb.ReplaceSegmentForm(2, 0, "xxkal", wsVernDef);
			var segformNode = pb.SegmentFormNode(2, 0);
			// Now it should parse as a wfic.
			var linkNode = segformNode.SelectSingleNode("AnnotationType34/Link");
			linkNode.Attributes["guid"].Value = ParagraphBuilder.WficGuid;
			linkNode.Attributes["name"].Value = "Wordform In Context";

			expectedOccurrences.Remove("xxkal" + wsDe.ToString());
			expectedOccurrences["xxkal" + wsVernDef.ToString()] += 1;
			pb.RebuildParagraphContentFromAnnotations(true);
			pb.ParseParagraph();
			int wsAnalysis2_0;
			FdoValidator.GetTssStringValue(tapb, 2, 0, out wsAnalysis2_0);
			Assert.AreEqual(Cache.DefaultVernWs, wsAnalysis2_0, "Precondition: expected to have default vern ws.");
			FdoValidator.ValidateCbaWordToBaselineWord(tapb, 2, 0);
			// validate the rest.
			tapb.ValidateAnnotations();
			CheckExpectedWordformsAndOccurrences(pb.ActualParagraph, expectedOccurrences);
		}

		/// <summary>
		/// Add a new ws alternative to an existing wordform.
		/// Then add another alternative that already has an existing hvo.
		/// </summary>
		[Ignore("LT-10147: Need to do this when we get more serious about merging wordform information.")]
		[Test]
		public void SimpleAnalyses_SimpleEdits_MultipleWritingSystemsParagraph_LT10147()
		{
		}

		/// <summary>
		/// Test making a phrase.
		/// </summary>
		[Test]
		public void Phrase_Make()
		{
			// 1. Make Phrases
			IList<int[]> secondaryPathsToJoinWords = new List<int[]>();
			IList<int[]> secondaryPathsToBreakPhrases = new List<int[]>();

			ParagraphBuilder pb = new ParagraphBuilder(m_textsDefn, m_text1, (int) Text1ParaIndex.PhraseWordforms);
			ParagraphAnnotatorForParagraphBuilder tapb = new ParagraphAnnotatorForParagraphBuilder(pb);
			XmlNode paraDef0 = pb.ParagraphDefinition.CloneNode(true);

			XmlNode paraDef_afterJoin1_2;
			pb.ParseParagraph();
			// xxxpus xxxyalola xxxnihimbilira. xxxpus xxxyalola [xxxhesyla xxxnihimbilira]. xxxpus xxxyalola xxxnihimbilira
			tapb.MergeAdjacentAnnotations(1, 2, secondaryPathsToJoinWords, secondaryPathsToBreakPhrases);
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
			// 1. Make Phrases
			IList<int[]> secondaryPathsToJoinWords = new List<int[]>();
			IList<int[]> secondaryPathsToBreakPhrases = new List<int[]>();
			//Thist test proved to be incapable of being Undone, since further tests relied on state this test affected
			//the following two variables duplicate member variables to isolate changes to this test.
			XmlDocument donTouchIt = new XmlDocument();
			IText noLoToque = LoadTestText(
				Path.Combine("FDO",
				 Path.Combine("FDOTests",
				  Path.Combine("TestData", "ParagraphParserTestTexts.xml"))),
					1, donTouchIt);

			ParagraphBuilder pb = new ParagraphBuilder(donTouchIt, noLoToque, (int)Text1ParaIndex.PhraseWordforms);
			ParagraphAnnotatorForParagraphBuilder tapb = new ParagraphAnnotatorForParagraphBuilder(pb);
			// go ahead and parse the text generating real wordforms to be more like a real user situation (LT-7974)
			// and reset the concordance to simulate the situation where the user hasn't loaded ConcordanceWords yet.
			ParagraphParser.ParseText(noLoToque.ContentsOA);
			pb.ResyncExpectedAnnotationIds();

			// first do a basic phrase (without secondary phrases (guesses)), which should not delete "xxxpus" or "xxxyalola"
			// since they occur later in the text.
			// [xxxpus xxxyalola] xxxnihimbilira. xxxpus xxxyalola xxxhesyla xxxnihimbilira. xxxpus xxxyalola xxxnihimbilira
			IWfiWordform wf1 = pb.ActualParagraph.SegmentsOS[0].AnalysesRS[0].Wordform;
			IWfiWordform wf2 = pb.ActualParagraph.SegmentsOS[0].AnalysesRS[1].Wordform;
			tapb.MergeAdjacentAnnotations(0, 0, secondaryPathsToJoinWords, secondaryPathsToBreakPhrases);
			// after the merge, make sure the wordforms for xxxpus and xxxyalola are still valid
			Assert.IsTrue(wf1.IsValidObject, "expected xxxpus to still be valid");
			Assert.IsTrue(wf2.IsValidObject, "expected xxxyalola to still be valid");
			tapb.ValidateAnnotations(true);

			// now break the phrase and make sure we delete this wordform, b/c it is not occurring elsewhere
			pb.ResyncExpectedAnnotationIds();
			// \xxxpus\ \xxxyalola\ xxxnihimbilira. xxxpus xxxyalola xxxhesyla xxxnihimbilira. xxxpus xxxyalola xxxnihimbilira
			IWfiWordform wf3 = pb.ActualParagraph.SegmentsOS[0].AnalysesRS[0].Wordform;
			secondaryPathsToJoinWords.Clear();
			secondaryPathsToBreakPhrases.Clear();
			tapb.BreakPhrase(0, 0, secondaryPathsToBreakPhrases, secondaryPathsToJoinWords, null);
			Assert.IsFalse(wf3.IsValidObject, "expected 'xxxpus xxxyalola' to be deleted.");
			tapb.ValidateAnnotations(true);

			// rejoin the first phrase, parse the text, and break the first phrase
			// [xxxpus xxxyalola] xxxnihimbilira. {xxxpus xxxyalola} xxxhesyla xxxnihimbilira. {xxxpus xxxyalola} xxxnihimbilira
			secondaryPathsToJoinWords.Add(new int[2] { 1, 0 }); // {xxxpus xxxyalola} xxxhesyla
			secondaryPathsToJoinWords.Add(new int[2] { 2, 0 }); // {xxxpus xxxyalola} xxxnihimbilira
			tapb.MergeAdjacentAnnotations(0, 0, secondaryPathsToJoinWords, secondaryPathsToBreakPhrases);
			wf1 = pb.ActualParagraph.SegmentsOS[0].AnalysesRS[0].Wordform;
			tapb.ReparseParagraph();
			// just break the new phrase (and the leave existing guesses)
			// \xxxpus\ \xxxyalola\ xxxnihimbilira. {xxxpus xxxyalola} xxxhesyla xxxnihimbilira. {xxxpus xxxyalola} xxxnihimbilira
			secondaryPathsToJoinWords.Clear();
			tapb.BreakPhrase(0, 0, secondaryPathsToBreakPhrases, secondaryPathsToJoinWords, null);
			Assert.IsTrue(wf1.IsValidObject, "expected 'xxxpus xxxyalola' to still be valid");
			tapb.ValidateAnnotations(true);
			// xxxpus xxxyalola xxxnihimbilira. \xxxpus\ \xxxyalola\ xxxhesyla xxxnihimbilira. {xxxpus xxxyalola} xxxnihimbilira
			tapb.BreakPhrase(1, 0, secondaryPathsToBreakPhrases, secondaryPathsToJoinWords, null);
			Assert.IsTrue(wf1.IsValidObject, "expected 'xxxpus xxxyalola' to still be valid");
			tapb.ValidateAnnotations(true);
			// xxxpus xxxyalola xxxnihimbilira. xxxpus xxxyalola xxxhesyla xxxnihimbilira. \xxxpus\ \xxxyalola\ xxxnihimbilira
			tapb.BreakPhrase(2, 0, secondaryPathsToBreakPhrases, secondaryPathsToJoinWords, null);
			Assert.IsFalse(wf1.IsValidObject, "expected 'xxxpus xxxyalola' to be deleted");
			tapb.ValidateAnnotations(true);

			// now do two joins, the second resulting in deletion of a wordform.
			// xxxpus xxxyalola xxxnihimbilira. xxxpus [xxxyalola xxxhesyla] xxxnihimbilira. xxxpus xxxyalola xxxnihimbilira
			// xxxpus xxxyalola xxxnihimbilira. xxxpus [xxxyalola xxxhesyla xxxnihimbilira]. xxxpus xxxyalola xxxnihimbilira
			ParagraphParser.ParseText(noLoToque.ContentsOA);
			pb.ResyncExpectedAnnotationIds();
			tapb.MergeAdjacentAnnotations(1, 1, secondaryPathsToJoinWords, secondaryPathsToBreakPhrases);
			IWfiWordform wf4 = pb.ActualParagraph.SegmentsOS[1].AnalysesRS[1].Wordform;
			tapb.MergeAdjacentAnnotations(1, 1, secondaryPathsToJoinWords, secondaryPathsToBreakPhrases);
			// this merge should have deleted 'xxxyalola xxxhesyla'
			Assert.IsFalse(wf4.IsValidObject, "expected 'xxxyalola xxxhesyla' to be deleted.");
			tapb.ValidateAnnotations(true);
		}

		/// <summary>
		/// Test the annotations for paragraph with phrase-wordform annotations.
		/// </summary>
		[Test]
		//[Ignore("FWC-16: this test causes NUnit to hang in fixture teardown - need more investigation")]
		public void Phrase_MakeAndBreak()
		{
			// 1. Make Phrases
			IList<int[]> secondaryPathsToJoinWords = new List<int[]>();
			IList<int[]> secondaryPathsToBreakPhrases = new List<int[]>();

			ParagraphBuilder pb = new ParagraphBuilder(m_textsDefn, m_text1, (int)Text1ParaIndex.PhraseWordforms);
			ParagraphAnnotatorForParagraphBuilder tapb = new ParagraphAnnotatorForParagraphBuilder(pb);
			pb.ParseParagraph();

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

			// JohnT: in 6.0, making the larger phrase would apparently get rid of the guessed phrase.
			// in 7.0+ there's no distinction between a guess and user-made phrase, so once it exists it doesn't go away.
			// [xxxpus xxxyalola xxxnihimbilira]. [xxxpus xxxyalola] xxxhesyla xxxnihimbilira. {xxxpus xxxyalola xxxnihimbilira}.
			secondaryPathsToJoinWords.Clear();
			secondaryPathsToBreakPhrases.Clear();
			// 7.0+ secondaryPathsToBreakPhrases.Add(new int[2] { 1, 0 }); // \xxxpus\ \xxxyalola\ xxxhesyla
			secondaryPathsToJoinWords.Add(new int[2] { 2, 0 }); // {{xxxpus xxxyalola} xxxnihimbilira}
			tapb.MergeAdjacentAnnotations(0, 0, secondaryPathsToJoinWords, secondaryPathsToBreakPhrases);
			tapb.ValidateAnnotations();

			// [xxxpus xxxyalola xxxnihimbilira]. [xxxpus xxxyalola] xxxhesyla xxxnihimbilira. {xxxpus xxxyalola xxxnihimbilira}
			// 7.0+ nothing to do here; it's in this state after the previous change.
			//secondaryPathsToJoinWords.Clear();
			//secondaryPathsToBreakPhrases.Clear();
			//tapb.MergeAdjacentAnnotations(1, 0, secondaryPathsToJoinWords, secondaryPathsToBreakPhrases);
			//tapb.ValidateAnnotations();

			// 2. Break Phrases.
			// [xxxpus xxxyalola xxxnihimbilira]. \xxxpus\ \xxxyalola\ xxxhesyla xxxnihimbilira. {xxxpus xxxyalola xxxnihimbilira}
			secondaryPathsToJoinWords.Clear();
			secondaryPathsToBreakPhrases.Clear();
			tapb.BreakPhrase(1, 0, secondaryPathsToBreakPhrases, secondaryPathsToJoinWords, null);
			tapb.ValidateAnnotations();

			// [\xxxpus\ \xxxyalola\ \xxxnihimbilira\]. xxxpus xxxyalola xxxhesyla xxxnihimbilira. {\xxxpus\ \xxxyalola\ \xxxnihimbilira\}
			secondaryPathsToBreakPhrases.Clear();
			// 7.0+ secondaryPathsToBreakPhrases.Add(new int[2] { 2, 0 }); // {\xxxpus\ \xxxyalola\ \xxxnihimbilira\}
			tapb.BreakPhrase(0, 0, secondaryPathsToBreakPhrases, secondaryPathsToJoinWords, null);
			tapb.ValidateAnnotations();

			// 7.0+ breaking the guessed phrase has to be an extra step.
			tapb.BreakPhrase(2, 0, secondaryPathsToBreakPhrases, secondaryPathsToJoinWords, null);
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
			// In 6.0, apparently we would re-guess the shorter pus yalola phrase. In 7.0+, breaking a phrase does not
			// produce a new parse and new phrase guesses. Otherwise, if the longer phrase still existed somewhere else,
			// it would be guessed again! Don't see how this ever worked right, except that after breaking the phrase the
			// user would usually annotate the parts before there was occasion to re-parse.
			// 7.0+ secondaryPathsToJoinWords.Add(new int[2] { 2, 0 }); // {xxxpus xxxyalola} xxxnihimbilira
			tapb.BreakPhrase(2, 0, secondaryPathsToBreakPhrases, secondaryPathsToJoinWords, "xxxpus xxxyalola");
			tapb.ValidateAnnotations();
		}

		/// <summary>
		/// Test editing the text after making a phrase.
		/// </summary>
		[Test]
		public void Phrase_SimpleEdits_LT6244()
		{
			// 1. Make Phrases
			IList<int[]> secondaryPathsToJoinWords = new List<int[]>();
			IList<int[]> secondaryPathsToBreakPhrases = new List<int[]>();

			ParagraphBuilder pb = new ParagraphBuilder(m_textsDefn, m_text1, (int)Text1ParaIndex.PhraseWordforms);
			ParagraphAnnotatorForParagraphBuilder tapb = new ParagraphAnnotatorForParagraphBuilder(pb);
			pb.ParseParagraph();

			// first do a basic phrase (without secondary phrases (guesses))
			// xxxpus xxxyalola xxxnihimbilira. xxxpus xxxyalola [xxxhesyla xxxnihimbilira]. xxxpus xxxyalola xxxnihimbilira
			tapb.MergeAdjacentAnnotations(1, 2, secondaryPathsToJoinWords, secondaryPathsToBreakPhrases);
			tapb.ValidateAnnotations(true);

			// edit the second word in the phrase.
			pb.ReplaceSegmentForm("xxxhesyla xxxnihimbilira", 0, "xxxhesyla xxxra");
			pb.BreakPhraseAnnotation(1, 2);  // this edit should break the phrase back into words.
			tapb.ValidateAnnotations();
		}

		/// <summary>
		/// This tests some stuff that was dubious in 6.0 but pretty much automatic in 7.0+. We may want to drop it.
		/// </summary>
		[Test]
		public void SparseSegmentAnalyses_FreeformAnnotations_LT7318()
		{
			// Make some analyses linked to the same LexSense.
			//<!--xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.-->
			ParagraphBuilder pb = new ParagraphBuilder(m_textsDefn, m_text1, (int)Text1ParaIndex.SimpleSegmentPara);
			var segments = pb.ActualParagraph.SegmentsOS;
			Assert.AreEqual(3, segments.Count, "Unexpected number of senses.");

			// Load free form annotations for segments. shouldn't have any.
			Assert.That(segments[0].FreeTranslation.AvailableWritingSystemIds.Length, Is.EqualTo(0));
			Assert.That(segments[1].FreeTranslation.AvailableWritingSystemIds.Length, Is.EqualTo(0));
			Assert.That(segments[2].FreeTranslation.AvailableWritingSystemIds.Length, Is.EqualTo(0));

			// Try adding some freeform translations to third segment.
			segments[2].FreeTranslation.AnalysisDefaultWritingSystem =
				Cache.TsStrFactory.MakeString("Segment2: Freeform translation.", Cache.DefaultAnalWs);
			segments[2].LiteralTranslation.AnalysisDefaultWritingSystem =
				Cache.TsStrFactory.MakeString("Segment2: Literal translation.", Cache.DefaultAnalWs);

			// make sure the other segments don't have freeform annotations.
			Assert.That(segments[0].FreeTranslation.AvailableWritingSystemIds.Length, Is.EqualTo(0));
			Assert.That(segments[1].FreeTranslation.AvailableWritingSystemIds.Length, Is.EqualTo(0));

			// reparse the paragraph and make sure nothing changed.
			ParagraphAnnotatorForParagraphBuilder tapb = new ParagraphAnnotatorForParagraphBuilder(pb);
			tapb.ReparseParagraph();
			segments = (pb.ActualParagraph as IStTxtPara).SegmentsOS;

			Assert.That(segments[0].FreeTranslation.AvailableWritingSystemIds.Length, Is.EqualTo(0));
			Assert.That(segments[1].FreeTranslation.AvailableWritingSystemIds.Length, Is.EqualTo(0));
			Assert.That(segments[2].FreeTranslation.AnalysisDefaultWritingSystem.Text, Is.EqualTo("Segment2: Freeform translation."));
			Assert.That(segments[2].LiteralTranslation.AnalysisDefaultWritingSystem.Text, Is.EqualTo("Segment2: Literal translation."));
		}

		/// <summary>
		/// This is a test adapted from 6.0, where I think it mainly tested various virtual properties that no longer
		/// exist. It sets up some of the data that would be needed to find example sentences.
		/// </summary>
		[Test]
		public void FindExampleSentences()
		{
			// Make some analyses linked to the same LexSense.
			ParagraphBuilder pb = new ParagraphBuilder(m_textsDefn, m_text1, (int)Text1ParaIndex.SimpleSegmentPara);
			ParagraphAnnotatorForParagraphBuilder tapb = new ParagraphAnnotatorForParagraphBuilder(pb);
			ParagraphParser.ParseParagraph(pb.ActualParagraph);
			// Create a new lexical entry and sense.
			int clsidForm;
			string formLexEntry = "xnihimbilira";
			var morphTypeRepository = Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>();
			var rootMorphType = morphTypeRepository. GetObject(MoMorphTypeTags.kguidMorphRoot);
			ITsString tssLexEntryForm = TsStringUtils.MakeTss(formLexEntry, Cache.DefaultVernWs);
			var entryFactory = Cache.ServiceLocator.GetInstance<ILexEntryFactory>();
			ILexEntry xnihimbilira_Entry = entryFactory.Create(rootMorphType, tssLexEntryForm, "xnihimbilira.sense1", null);

			ILexSense xnihimbilira_Sense1 = xnihimbilira_Entry.SensesOS[0];
			var senseFactory = Cache.ServiceLocator.GetInstance<ILexSenseFactory>();
			ILexSense xnihimbilira_Sense2 = senseFactory.Create(xnihimbilira_Entry, null, "xnihimbilira.sense2");
			//<!--xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus xxxyalola. xxxhesyla xxxnihimbilira.-->
			ArrayList moForms = new ArrayList();
			moForms.Add("xx");
			moForms.Add(xnihimbilira_Entry.LexemeFormOA);
			// 1. Establish first analysis with Sense1
			IWfiAnalysis wfiAnalysis1 = tapb.BreakIntoMorphs(1, 0, moForms);
			tapb.SetMorphSense(1, 0, 1, xnihimbilira_Sense1);
			Assert.That(xnihimbilira_Sense1.ReferringObjects, Has.Count.EqualTo(1));
			Assert.That(xnihimbilira_Sense2.ReferringObjects, Has.Count.EqualTo(0));

			IWfiAnalysis waCba1_0 = pb.ActualParagraph.SegmentsOS[1].AnalysesRS[0] as IWfiAnalysis;
			Assert.IsNotNull(waCba1_0,
							 String.Format("Unexpected class({0}) of Analysis({1}) for cba0.",
							 pb.ActualParagraph.SegmentsOS[0].AnalysesRS[2].GetType(),
							 pb.ActualParagraph.SegmentsOS[0].AnalysesRS[2].Hvo));
			Assert.AreEqual(xnihimbilira_Sense1, waCba1_0.MorphBundlesOS[1].SenseRA);
			Assert.That(waCba1_0.ReferringObjects, Has.Count.EqualTo(1)); // one ref to the analysis (from segment 0)
			var wordform = (IWfiWordform)waCba1_0.Owner;
			Assert.That(wordform.FullConcordanceCount, Is.EqualTo(3)); // unchanged even though one is now an analysis

			// 2. Establish word gloss on the existing analysis.
			string wordGloss1;
			tapb.SetDefaultWordGloss(2, 1, wfiAnalysis1, out wordGloss1);
			Assert.That(wordform.FullConcordanceCount, Is.EqualTo(3)); // analysis and gloss and unchanged wordform all count

			var wgCba2_1 = pb.ActualParagraph.SegmentsOS[2].AnalysesRS[1] as IWfiGloss;
			Assert.IsNotNull(wgCba2_1,
							 String.Format("Unexpected class({0}) of InstanceOf({1}) for cba1.",
							 pb.ActualParagraph.SegmentsOS[2].AnalysesRS[1].GetType(), pb.ActualParagraph.SegmentsOS[2].AnalysesRS[1].Hvo));
			var waCba2_1 = wgCba2_1.Owner as IWfiAnalysis;
			Assert.AreEqual(xnihimbilira_Sense1.Hvo, waCba2_1.MorphBundlesOS[1].SenseRA.Hvo);

			// 3. establish a new analysis with Sense1.
			IWfiAnalysis wfiAnalysis2 = tapb.BreakIntoMorphs(0, 2, moForms); // xxxnihimbilira (first occurrence)
			tapb.SetMorphSense(0, 2, 1, xnihimbilira_Sense1);
			Assert.That(wordform.FullConcordanceCount, Is.EqualTo(3));

			var waCba0_2 = pb.ActualParagraph.SegmentsOS[0].AnalysesRS[2] as IWfiAnalysis;
			Assert.IsNotNull(waCba0_2,
							 String.Format("Unexpected class({0}) of InstanceOf({1}) for cba0.",
							 pb.ActualParagraph.SegmentsOS[2].AnalysesRS[1].GetType(), pb.ActualParagraph.SegmentsOS[2].AnalysesRS[1].Hvo));
			Assert.AreEqual(xnihimbilira_Sense1.Hvo, waCba0_2.MorphBundlesOS[1].SenseRA.Hvo);
			Assert.That(xnihimbilira_Sense1.ReferringObjects, Has.Count.EqualTo(2));
			Assert.That(xnihimbilira_Sense2.ReferringObjects, Has.Count.EqualTo(0));

			// 4. change an existing sense to sense2.
			tapb.SetMorphSense(0, 2, 1, xnihimbilira_Sense2);
			Assert.That(xnihimbilira_Sense1.ReferringObjects, Has.Count.EqualTo(1));
			Assert.That(xnihimbilira_Sense2.ReferringObjects, Has.Count.EqualTo(1));

			waCba0_2 = pb.ActualParagraph.SegmentsOS[0].AnalysesRS[2] as IWfiAnalysis;
			Assert.IsNotNull(waCba0_2,
							 String.Format("Unexpected class({0}) of InstanceOf({1}) for cba0.",
							 pb.ActualParagraph.SegmentsOS[2].AnalysesRS[1].GetType(), pb.ActualParagraph.SegmentsOS[2].AnalysesRS[1].Hvo));
			Assert.AreEqual(xnihimbilira_Sense2, waCba0_2.MorphBundlesOS[1].SenseRA);

			// do multiple occurrences of the same sense in the same segment.
			IWfiAnalysis wfiAnalysis3 = tapb.BreakIntoMorphs(0, 0, moForms); // break xxxpus into xx xnihimbilira (for fun).
			tapb.SetMorphSense(0, 0, 1, xnihimbilira_Sense2);
			Assert.That(xnihimbilira_Sense2.ReferringObjects, Has.Count.EqualTo(2));

			// reparse paragraph and make sure important stuff doesn't change
			tapb.ReparseParagraph();
			Assert.That(xnihimbilira_Sense1.ReferringObjects, Has.Count.EqualTo(1));
			Assert.That(xnihimbilira_Sense2.ReferringObjects, Has.Count.EqualTo(2));
		}

		/// <summary>
		/// Test editing simple segment paragraph with sparse analyses.
		/// xxxpus [xxxyalola] xxxnihimbilira. xxxnihimbilira [xxxpus] xxxyalola. xxxhesyla [xxxnihimbilira].
		/// </summary>
		[Test]
		public void SparseAnalyses_SimpleEdits_SimpleSegmentParagraph_DuplicateWordforms()
		{
			// First set sparse analyses on wordforms that have multiple occurrences.
			ParagraphBuilder pb = new ParagraphBuilder(m_textsDefn, m_text1, (int)Text1ParaIndex.SimpleSegmentPara);
			ParagraphAnnotatorForParagraphBuilder tapb = new ParagraphAnnotatorForParagraphBuilder(pb);
			pb.ParseParagraph();
			IWfiGloss gloss_xxxyalola0_1 = tapb.SetDefaultWordGloss("xxxyalola", 0);		// gloss first occurrence.
			IWfiGloss gloss_xxxpus1_1 = tapb.SetDefaultWordGloss("xxxpus", 1);			// gloss second occurrence.
			IWfiGloss gloss_xxxnihimbilira2_1 = tapb.SetDefaultWordGloss("xxxnihimbilira", 2);	// gloss third occurrence.
			pb.ParseParagraph();
			var actualAnalysis_xxxyalola0_1 = tapb.GetAnalysis(0, 1);
			var actualAnalysis_xxxpus1_1 = tapb.GetAnalysis(1, 1);
			var actualAnalysis_xxxnihimbilira2_1 = tapb.GetAnalysis(2, 1);
			Assert.AreEqual(gloss_xxxyalola0_1, actualAnalysis_xxxyalola0_1);
			Assert.AreEqual(gloss_xxxpus1_1, actualAnalysis_xxxpus1_1);
			Assert.AreEqual(gloss_xxxnihimbilira2_1, actualAnalysis_xxxnihimbilira2_1);
			// verify the rest
			tapb.ValidateAnnotations();
			// Replace some occurrences of these wordforms from the text to validate the analysis does not show up on the wrong occurrence.
			// Remove the first occurrence of 'xxxnihimbilira'; the (newly) second occurrence should still have the gloss.
			pb.ReplaceSegmentForm("xxxnihimbilira", 0, "");
			pb.RebuildParagraphContentFromAnnotations();
			pb.ParseParagraph();
			actualAnalysis_xxxnihimbilira2_1 = tapb.GetAnalysis(2, 1);
			Assert.AreEqual(gloss_xxxnihimbilira2_1, actualAnalysis_xxxnihimbilira2_1);
			tapb.ValidateAnnotations();
			// Remove first occurrence of 'xxxpus'; the next one should still have the gloss.
			pb.ReplaceSegmentForm("xxxpus", 0, "");
			pb.RebuildParagraphContentFromAnnotations();
			pb.ParseParagraph();
			actualAnalysis_xxxpus1_1 = tapb.GetAnalysis(1, 1);
			Assert.AreEqual(gloss_xxxpus1_1, actualAnalysis_xxxpus1_1);
			tapb.ValidateAnnotations();
			//SparseAnalyses_SimpleEdits_SimpleSegmentParagraph_DuplicateWordforms_RemoveSegment_LT5376()
			//SparseAnalyses_SimpleEdits_SimpleSegmentParagraph_DuplicateWordforms_AddWhitespace_LT5313()
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		[Ignore("See LT-5376 for details.")]
		public void SparseAnalyses_SimpleEdits_SimpleSegmentParagraph_DuplicateWordforms_RemoveSegment_LT5376()
		{
			// First set sparse analyses on wordforms that have multiple occurrences.
			ParagraphBuilder pb = new ParagraphBuilder(m_textsDefn, m_text1, (int)Text1ParaIndex.SimpleSegmentPara);
			pb.ParseParagraph();
			ParagraphAnnotatorForParagraphBuilder tapb = new ParagraphAnnotatorForParagraphBuilder(pb);
			tapb.SetDefaultWordGloss("xxxyalola", 0);		// gloss first occurrence.
			tapb.ValidateAnnotations();
			// Remove first sentence containing 'xxxyalola'; its annotation should be removed.
			pb.RemoveSegment(0);
			pb.RebuildParagraphContentFromAnnotations();
			pb.ParseParagraph();
			tapb.ValidateAnnotations();
		}

		/// <summary>
		///			1		  2		 3		 4		  5		  6		 7		  8		  9
		/// 0123456 789012345 67890123456789012345678901234567 890123 4567890123456789012345 67890123456789 0123456789
		/// xxxpus [xxxyalola] xxxnihimbilira. xxxnihimbilira [xxxpus] xxxyalola. xxxhesyla [xxxnihimbilira].
		/// xxxpus  [xxxyalola] xxxnihimbilira. xxxnihimbilira [xxxpus] xxxyalola. xxxhesyla [xxxnihimbilira].
		/// </summary>
		[Test]
		public void SparseAnalyses_SimpleEdits_SimpleSegmentParagraph_DuplicateWordforms_AddWhitespace_LT5313()
		{
			// First set sparse analyses on wordforms that have multiple occurrences.
			ParagraphBuilder pb = new ParagraphBuilder(m_textsDefn, m_text1, (int)Text1ParaIndex.SimpleSegmentPara);
			pb.ParseParagraph();
			ParagraphAnnotatorForParagraphBuilder tapb = new ParagraphAnnotatorForParagraphBuilder(pb);
			string gloss;
			var gloss0_1 = tapb.SetDefaultWordGloss(0, 1, out gloss);   // xxxyalola 1
			var gloss1_1 = tapb.SetDefaultWordGloss(1, 1, out gloss);   // xxxpus 2
			var gloss2_1 = tapb.SetDefaultWordGloss(2, 1, out gloss);   // xxxnihimbilira 3
			var occ0_1_beforeSpace = pb.GetExpectedAnalysis(0, 1);
			var occ1_1_beforeSpace = pb.GetExpectedAnalysis(1, 1);
			var occ2_1_beforeSpace = pb.GetExpectedAnalysis(2, 1);
			tapb.ValidateAnnotations(); // precondition testing.
			// Append whitespace in the text, and see if the analyses still show up in the right place
			// (cf. LT-5313).
			pb.ReplaceTrailingWhitepace(0, 0, 1);
			pb.RebuildParagraphContentFromAnnotations();
			pb.ParseParagraph();
			Assert.AreEqual(gloss0_1, tapb.GetAnalysis(0, 1));
			Assert.AreEqual(gloss1_1, tapb.GetAnalysis(1, 1));
			Assert.AreEqual(gloss2_1, tapb.GetAnalysis(2, 1));
			tapb.ValidateAnnotations();
		}

		/// <summary>
		/// Xxxpus xxxyalola xxxnihimbilira. Xxxnihimbilira xxxpus Xxxyalola. Xxxhesyla XXXNIHIMBILIRA.
		/// </summary>
		[Test]
		public void SparseAnalyses_NoEdits_MixedCaseWordformsParagraph()
		{
			// First set sparse analyses on wordforms that have multiple occurrences.
			ParagraphBuilder pb = new ParagraphBuilder(m_textsDefn, m_text1, (int)Text1ParaIndex.MixedCases);
			ParagraphAnnotatorForParagraphBuilder tapb = new ParagraphAnnotatorForParagraphBuilder(pb);
			pb.ParseParagraph();

			// set the corresponding annotations to lowercase wordforms.
			IWfiWordform wf_xxxpus0_0 = tapb.SetAlternateCase("Xxxpus", 0, StringCaseStatus.allLower);
			IWfiWordform wf_xxxnihimbilira1_0 = tapb.SetAlternateCase("Xxxnihimbilira", 0, StringCaseStatus.allLower);
			IWfiWordform wf_xxxhesyla2_0 = tapb.SetAlternateCase("Xxxhesyla", 0, StringCaseStatus.allLower);
			IWfiWordform wf_xxxnihimbilira2_1 = tapb.SetAlternateCase("XXXNIHIMBILIRA", 0, StringCaseStatus.allLower);
			pb.ParseParagraph();
			Assert.AreEqual(wf_xxxpus0_0, tapb.GetAnalysis(0, 0));
			Assert.AreEqual(wf_xxxnihimbilira1_0, tapb.GetAnalysis(1, 0));
			Assert.AreEqual(wf_xxxhesyla2_0, tapb.GetAnalysis(2, 0));
			Assert.AreEqual(wf_xxxnihimbilira2_1, tapb.GetAnalysis(2, 1));
			tapb.ValidateAnnotations();
		}

		/// <summary>
		/// test that the paragraph parser can maintain sparse analyses in a simple paragraph.
		/// xxxpus xxxyalola xxxnihimbilira. xxxnihimbilira xxxpus [xxxyalola]. xxxhesyla [xxxnihimbilira].
		/// </summary>
		[Test]
		public void SparseAnalyses_NoEdits_SimpleSegmentParagraph()
		{
			// First set sparse analyses on wordforms that have multiple occurrences.
			ParagraphBuilder pb = new ParagraphBuilder(m_textsDefn, m_text1, (int)Text1ParaIndex.SimpleSegmentPara);
			ParagraphAnnotatorForParagraphBuilder tapb = new ParagraphAnnotatorForParagraphBuilder(pb);
			pb.ParseParagraph();
			string gloss;
			IWfiGloss gloss_xxxyalola1_2 = tapb.SetDefaultWordGloss(1, 2, out gloss);
			IWfiGloss gloss_xxxnihimbilira2_1 = tapb.SetDefaultWordGloss(2, 1, out gloss);

			// now parse through the text and make sure our two glosses are maintained.
			pb.ParseParagraph();
			Assert.AreEqual(gloss_xxxyalola1_2, tapb.GetAnalysis(1, 2));
			Assert.AreEqual(gloss_xxxnihimbilira2_1, tapb.GetAnalysis(2, 1));
			// validate the rest of the stuff.
			tapb.ValidateAnnotations();
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void CheckValidGuessesAfterInsertNewWord_LT8467()
		{
			//NOTE: The new test paragraphs need to have all new words w/o duplicates so we can predict the guesses
			//xxxcrayzee xxxyouneek xxxsintents.

			// copy a text of first paragraph into a new paragraph to generate guesses.
			IStTxtPara paraGlossed = m_text1.ContentsOA.AddNewTextPara("Normal");
			IStTxtPara paraGuessed = m_text1.ContentsOA.AddNewTextPara("Normal");

			paraGlossed.Contents = TsStringUtils.MakeTss("xxxcrayzee xxxyouneek xxxsintents.", Cache.DefaultVernWs);
			paraGuessed.Contents = paraGlossed.Contents;

			// collect expected guesses from the glosses in the first paragraph.
			ParagraphAnnotator paGlossed = new ParagraphAnnotator(paraGlossed);
			ParagraphParser.ParseText(m_text1.ContentsOA);
			IList<IWfiGloss> expectedGuesses = paGlossed.SetupDefaultWordGlosses();

			// then verify we've created guesses for the new text.
			ParagraphAnnotator paGuessed = new ParagraphAnnotator(paraGuessed);

			IList<IWfiGloss> expectedGuessesBeforeEdit = expectedGuesses;
			ValidateGuesses(expectedGuessesBeforeEdit, paraGuessed);

			// now edit the paraGuessed and expected Guesses.
			paraGuessed.Contents = TsStringUtils.MakeTss("xxxcrayzee xxxguessless xxxyouneek xxxsintents.", Cache.DefaultVernWs);
			IList<IWfiGloss> expectedGuessesAfterEdit = new List<IWfiGloss>(expectedGuesses);
			// we don't expect a guess for the inserted word, so insert 0 after first twfic.
			expectedGuessesAfterEdit.Insert(1, null);

			// Note: we need to use ParseText rather than ReparseParagraph, because it uses
			// code to Reuse dummy annotations.
			ParagraphParser.ParseText(m_text1.ContentsOA);
			ValidateGuesses(expectedGuessesAfterEdit, paraGuessed);
		}

		private void ValidateGuesses(IList<IWfiGloss> expectedGuesses, IStTxtPara paraWithGuesses)
		{
			var segsParaGuesses = paraWithGuesses.SegmentsOS;
			int iExpectedGuess = 0;
			var GuessServices = new AnalysisGuessServices(Cache);
			foreach (var segParaGuesses in segsParaGuesses)
			{
				var segFormsParaGuesses = segParaGuesses.AnalysesRS;
				Assert.AreEqual(expectedGuesses.Count, segFormsParaGuesses.Count);
				int ianalysis = 0;
				foreach (var analysis in segFormsParaGuesses)
				{
					IAnalysis hvoGuessActual = null;
					if ((analysis.HasWordform))
					{
						// makes sense to guess
						hvoGuessActual = GuessServices.GetBestGuess(new AnalysisOccurrence(segParaGuesses, ianalysis));
						if (hvoGuessActual is NullWAG)
							hvoGuessActual = null;
					}
					Assert.AreEqual(expectedGuesses[iExpectedGuess], hvoGuessActual, "Guess mismatch");
					iExpectedGuess++;
					ianalysis++;
				}
			}
		}

		private void CheckExpectedWordformsAndOccurrences(IStTxtPara para, Dictionary<string, int> expectedOccurrences)
		{
			var wordforms = (from seg in para.SegmentsOS
						   from analysis in seg.AnalysesRS
						   where analysis.HasWordform
						   select analysis.Wordform).Distinct();
			var wfiRepo = para.Services.GetInstance<IWfiWordformRepository>();
			List<string> expectedWordforms = new List<string>(expectedOccurrences.Keys);
			foreach (string key in expectedWordforms)
			{
				int ichWs = key.IndexOfAny(new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' });
				string form = key.Substring(0, ichWs);
				int ws = Convert.ToInt32(key.Substring(ichWs));
				var wordform = wfiRepo.GetMatchingWordform(ws, form);
				Assert.That(wordform, Is.Not.Null, string.Format("Expected to find key {0} in ConcordanceWords.", key));
				Assert.That(wordforms, Has.Member(wordform),
					"The wordforms collected in last parse session doesn't contain the wordform "
					+ wordform.Form.VernacularDefaultWritingSystem.Text);

				// see if we match the expected occurrences.
				int occurrenceCount = wordform.OccurrencesInTexts.Count();
				Assert.AreEqual(expectedOccurrences[key], occurrenceCount,
								String.Format("Unexpected number of occurrences for wordform {0}", key));
			}
			Assert.AreEqual(expectedWordforms.Count, wordforms.Count(), "Word count mismatch.");
		}
	}
}
