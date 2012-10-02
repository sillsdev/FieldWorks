using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;
using System.Diagnostics;
using System.IO;
using System.Text;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.FDOTests;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Test.TestUtils;
using SIL.Utils;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// </summary>
	[TestFixture]
	public class InterlinearExporterTestsBase: InterlinearTestBase
	{
		protected FDO.IText m_text1 = null;
		XmlDocument m_textsDefn;
		XmlDocument m_exportedXml;
		XmlWriter m_writer = null;
		InterlinearExporter m_exporter = null;
		protected InterlinLineChoices m_choices = null;
		MemoryStream m_stream = null;
		InterlinVc m_vc = null;


		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			CheckDisposed();
			base.FixtureSetup();

			m_textsDefn = new XmlDocument();
			m_exportedXml = new XmlDocument();

			InstallVirtuals(Path.Combine(FwUtils.ksFlexAppName, Path.Combine("Configuration", "Main.xml")),
				new string[] { "SIL.FieldWorks.FDO.", "SIL.FieldWorks.IText." });
			m_vc = new InterlinVc(Cache);
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
				if (m_vc != null)
					m_vc.Dispose();
				if (m_writer != null)
					m_writer.Close();
				if (m_stream != null)
					m_stream.Close();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_vc = null;
			m_textsDefn = null;
			m_text1 = null;
			m_exportedXml = null;
			m_writer = null;
			m_exporter = null;
			m_choices = null;
			m_stream = null;

			base.Dispose(disposing);
		}

		[SetUp]
		public override void Initialize()
		{
			CheckDisposed();
			base.Initialize();

			m_text1 = SetupDataForText1();
			m_choices = new InterlinLineChoices(0, Cache.DefaultAnalWs, Cache.LangProject);
		}

		protected virtual FDO.IText SetupDataForText1()
		{
			return LoadTestText("LexText/Interlinear/ITextDllTests/InterlinearExporterTests.xml", 1, m_textsDefn);
		}

		[TearDown]
		public override void Exit()
		{
			CheckDisposed();

			m_text1 = null;
			m_choices = null;

			base.Exit();
		}

		protected XmlDocument ExportToXml(string mode)
		{
			using (m_stream = new MemoryStream())
			using (m_writer = new XmlTextWriter(m_stream, System.Text.Encoding.UTF8))
			{
				m_exporter = InterlinearExporter.Create(mode, Cache, m_writer, m_text1.ContentsOAHvo, m_choices, m_vc);
				m_exporter.WriteBeginDocument();
				m_exporter.ExportDisplay();
				m_exporter.WriteEndDocument();
				m_writer.Flush();
				m_stream.Seek(0, SeekOrigin.Begin);
				m_exportedXml.Load(m_stream);
			}
			m_writer = null;
			m_stream = null;
#pragma warning disable 219
			string xml = m_exportedXml.InnerXml;
#pragma warning restore 219
			return m_exportedXml;
		}

		protected XmlDocument ExportToXml()
		{
			return ExportToXml("xml");
		}

		internal class ExportedInterlinearReader
		{
			XmlNode m_exportedIText = null;
			InterlinLineChoices m_choices = null;

			internal ExportedInterlinearReader(XmlDocument exportedITextXml, InterlinLineChoices choices)
			{
				m_exportedIText = exportedITextXml.SelectSingleNode("/document");
				m_choices = choices;
			}

			internal InterlinLineChoices Choices
			{
				get { return m_choices; }
			}


			private XmlNodeList GetNodeList(XmlNode parent, string xpathVectorElement)
			{
				XmlNodeList nodeList = parent.SelectNodes(String.Format("{0}", xpathVectorElement));
				return nodeList;
			}

			private XmlNode GetSingleNode(XmlNode parent, string xpathElement, int xpathElementIndex)
			{
				XmlNode paraNode = parent.SelectSingleNode(String.Format("{0}[{1}]", xpathElement, xpathElementIndex));
				return paraNode;
			}

			internal XmlNode GetParaNode(int iPara)
			{
				XmlNode paraNode = GetSingleNode(m_exportedIText, "interlinear-text/paragraphs/paragraph", iPara + 1);
				return paraNode;
			}

			internal List<XmlNode> GetPhraseNodes(XmlNode para)
			{
				return NodeListToNodes(GetNodeList(para, "phrases/phrase"));
			}
			internal XmlNode GetPhraseNode(XmlNode para, int iPhrase)
			{
				XmlNode phraseNode = GetSingleNode(para, "phrases/phrase", iPhrase + 1);
				return phraseNode;
			}

			internal List<XmlNode> GetWordNodes(XmlNode phrase)
			{
				return NodeListToNodes(GetNodeList(phrase, "words/word"));
			}

			internal XmlNode GetWordNode(int iPara, int iPhrase, int iWord)
			{
				XmlNode wordNode = GetSingleNode(GetPhraseNode(GetParaNode(iPara), iPhrase), "words/word", iWord + 1);
				return wordNode;
			}

			internal List<XmlNode> GetMorphNodes(XmlNode wordNode)
			{
				return NodeListToNodes(wordNode.SelectNodes("./morphemes/morph"));
			}

			internal List<XmlNode> GetItems(XmlNode parent, string type)
			{
				return NodeListToNodes(GetNodeList(parent, String.Format("item[@type='{0}']", type)));
			}

			internal string GetItemInnerText(XmlNode parent, string type, string targetLang)
			{
				List<XmlNode> items = GetItems(parent, type);
				string str = "";
				foreach (XmlNode item in items)
				{
					string lang = XmlUtils.GetManditoryAttributeValue(item, "lang");
					if (lang == targetLang)
					{
						str = item.InnerText;
						break;
					}
				}
				return str;
			}
		}

		/// <summary>
		/// This class validates the actual exported xml against the expected StTxtPara.
		/// </summary>
		internal class ExportedParagraphValidator : ParagraphValidator
		{
			ExportedInterlinearReader m_reader = null;
			IStTxtPara m_para = null;
			protected FdoCache m_cache = null;
			InterlinLineChoices m_choices = null;

			internal ExportedParagraphValidator(ExportedInterlinearReader reader, IStTxtPara para)
			{
				m_reader = reader;
				m_choices = reader.Choices;
				m_para = para;
				m_cache = m_para.Cache;
			}

			bool IsLineEnabled(int lineChoice)
			{
				return m_choices.IndexOf(lineChoice) != -1;
			}

			protected override string GetParagraphContext(object expectedParaInfo)
			{
				return (expectedParaInfo as StTxtPara).Hvo.ToString();
			}

			protected override ArrayList GetExpectedSegments(object expectedParaInfo)
			{
				return new ArrayList((expectedParaInfo as StTxtPara).Segments.ToArray());
			}

			protected override ArrayList GetActualSegments(object actualParaInfo)
			{
				return new ArrayList(m_reader.GetPhraseNodes(actualParaInfo as XmlNode).ToArray());
			}

			protected override ArrayList GetExpectedSegmentForms(object expectedSegment)
			{
				return new ArrayList(m_para.SegmentForms((int)expectedSegment).ToArray());
			}

			protected override ArrayList GetActualSegmentForms(object actualSegment)
			{
				return new ArrayList(m_reader.GetWordNodes(actualSegment as XmlNode).ToArray());
			}

			protected void ValidateCmObjectGuidForNode(int hvoExpected, XmlNode nodeExported, string context, out string guidActual)
			{
				string msg = "Mismatched {0} in {1}.";
				ICmObject objExpected = CmObject.CreateFromDBObject(m_cache, hvoExpected);
				guidActual = XmlUtils.GetOptionalAttributeValue(nodeExported, "guid");
				Assert.IsNotNull(guidActual, "Missing guid in " + context);
				guidActual = guidActual.ToLowerInvariant();
				string guidExpected = objExpected.Guid.ToString().ToLowerInvariant();
				Assert.AreEqual(guidExpected, guidActual,
								String.Format(msg, "guid", context));
			}

			protected void ValidateMissingGuidForNode(XmlNode nodeExported, string context)
			{
				string guidActual = XmlUtils.GetOptionalAttributeValue(nodeExported, "guid");
				Assert.IsNull(guidActual, "Should not have guid for " + context);
			}

			protected override void ValidateSegForms(object expectedSegForm, object actualSegForm, string segFormContext)
			{
				string msg = "Mismatched {0} in {1}.";
				// Get the paragraph string corresponding to the annotation.
				ICmBaseAnnotation cbaExpected = CmBaseAnnotation.CreateFromDBObject(m_cache, (int)expectedSegForm);
				// first make sure we have a txt item.
				if (IsLineEnabled(InterlinLineChoices.kflidWord))
				{
					ITsString tssExpectedForm = m_para.Contents.UnderlyingTsString.GetSubstring(cbaExpected.BeginOffset, cbaExpected.EndOffset);
					string lang = "xkal";
					// Review: get WsLabel from tssExpectedForm.
					string actualForm = "";
					if (cbaExpected.AnnotationTypeRAHvo == TwficAnnotationType)
						actualForm = m_reader.GetItemInnerText(actualSegForm as XmlNode, "txt", lang);
					else if (cbaExpected.AnnotationTypeRAHvo == PunctuationAnnotationType)
						actualForm = m_reader.GetItemInnerText(actualSegForm as XmlNode, "punct", lang);
					Assert.AreEqual(tssExpectedForm.Text, actualForm,
							String.Format(msg, "word", segFormContext));
				}
				// if WordGloss is enabled, verify it.
				if (IsLineEnabled(InterlinLineChoices.kflidWordGloss))
				{
					string lang = "en";
					string actualWordGloss = m_reader.GetItemInnerText(actualSegForm as XmlNode, "gls", lang);
					if (cbaExpected.AnnotationTypeRAHvo == PunctuationAnnotationType)
					{
						// must be a punctuation (non-wfic)
						Assert.AreEqual("", actualWordGloss);
					}
					else
					{
						WfiGloss expectedGloss = null;
						int clsId = m_cache.GetClassOfObject(cbaExpected.InstanceOfRAHvo);
						if (clsId == WfiGloss.kclsidWfiGloss)
						{
							expectedGloss = new WfiGloss(m_cache, cbaExpected.InstanceOfRAHvo);
						}
						else if (clsId == WfiWordform.kclsidWfiWordform)
						{
							// should be a twfic so get its guess.
							StTxtPara.TwficInfo cbaInfo = new StTxtPara.TwficInfo(m_cache, cbaExpected.Hvo);
							int hvoExpectedGloss = cbaInfo.GetGuess();
							if (hvoExpectedGloss != 0)
								expectedGloss = new WfiGloss(m_cache, hvoExpectedGloss);
						}
						// TODO: There are cases for other classes (e.g. WfiAnalysis) but
						// the tests do not generate those right now, so we won't worry about them right now.
						if (expectedGloss != null)
							Assert.AreEqual(expectedGloss.Form.AnalysisDefaultWritingSystem, actualWordGloss);
						else
							Assert.AreEqual("", actualWordGloss);
					}
				}
				// validate morph bundle lines.
				if (IsLineEnabled(InterlinLineChoices.kflidMorphemes) ||
					IsLineEnabled(InterlinLineChoices.kflidLexEntries) ||
					IsLineEnabled(InterlinLineChoices.kflidLexGloss) ||
					IsLineEnabled(InterlinLineChoices.kflidLexPos))
				{
					// compare exported document to the LexEntries information in the WfiAnalysis
					int hvoWfiAnalysis = 0;
					if (cbaExpected.AnnotationTypeRAHvo != PunctuationAnnotationType)
						hvoWfiAnalysis = WfiAnalysis.GetWfiAnalysisFromInstanceOf(m_cache, cbaExpected.Hvo);
					List<XmlNode> morphNodes = m_reader.GetMorphNodes(actualSegForm as XmlNode);
					if (hvoWfiAnalysis == 0)
					{
						// make sure we don't have any morphs.
						Assert.IsEmpty(morphNodes);
					}
					else
					{
						IWfiAnalysis wfiAnalysis = WfiAnalysis.CreateFromDBObject(m_cache, hvoWfiAnalysis);
						foreach (WfiMorphBundle morphBundle in wfiAnalysis.MorphBundlesOS)
						{
							int iMorph = morphBundle.OwnOrd - 1;
							string morphContext = segFormContext + "/Morph(" + iMorph +")";
							XmlNode actualMorphNode = iMorph < morphNodes.Count ? morphNodes[iMorph] : null;
							if (actualMorphNode == null)
								Assert.Fail(String.Format(msg, "missing morph", morphContext));
							ITsString tssLexEntry = null;
							int hvoMorph = morphBundle.MorphRAHvo;
							if (hvoMorph != 0)
							{
								// first test the morph form
								if (IsLineEnabled(InterlinLineChoices.kflidMorphemes))
								{
									string actualMorphForm = m_reader.GetItemInnerText(actualMorphNode as XmlNode, "txt", "xkal");
									Assert.AreEqual(morphBundle.MorphRA.Form.VernacularDefaultWritingSystem,
													actualMorphForm,
													String.Format(msg, "morph/txt", morphContext));
								}

								// next test the lex entry
								if (IsLineEnabled(InterlinLineChoices.kflidLexEntries))
								{
									string actualLexEntry = m_reader.GetItemInnerText(actualMorphNode as XmlNode, "cf", "xkal");
									string actualHomograph = m_reader.GetItemInnerText(actualMorphNode as XmlNode, "hn", "en");
									string actualVariantTypes = m_reader.GetItemInnerText(actualMorphNode as XmlNode, "variantTypes", "en");
									tssLexEntry = InterlinDocChild.GetLexEntryTss(m_cache, morphBundle, m_cache.DefaultVernWs);
									Assert.AreEqual(tssLexEntry.Text, actualLexEntry + actualHomograph + actualVariantTypes,
													String.Format(msg, "morph/cf[hn|variantTypes]", morphContext));
								}

								if (IsLineEnabled(InterlinLineChoices.kflidLexGloss))
								{
									string actualLexGloss = m_reader.GetItemInnerText(actualMorphNode as XmlNode, "gls", "en");
									string expectedGloss = "";
									if (morphBundle.SenseRA != null && morphBundle.SenseRA.Gloss != null)
										expectedGloss = morphBundle.SenseRA.Gloss.AnalysisDefaultWritingSystem;
									Assert.AreEqual(expectedGloss, actualLexGloss,
													String.Format(msg, "morph/gls", morphContext));
								}
								if (IsLineEnabled(InterlinLineChoices.kflidLexPos))
								{
									string actualLexMsa = m_reader.GetItemInnerText(actualMorphNode as XmlNode, "msa", "en");
									string expectedMsa = "";
									if (morphBundle.SenseRA != null && morphBundle.SenseRA.MorphoSyntaxAnalysisRA != null)
										expectedMsa = morphBundle.SenseRA.MorphoSyntaxAnalysisRA.InterlinearAbbr;
									Assert.AreEqual(expectedMsa, actualLexMsa,
										String.Format(msg, "morph/msa", morphContext));
								}
							}
						}
						Assert.AreEqual(wfiAnalysis.MorphBundlesOS.Count, morphNodes.Count);
					}

				}
			}
		}

		protected void ValidateExportedParagraph(XmlDocument exportedXml, InterlinLineChoices choices, IStTxtPara para)
		{
			ExportedInterlinearReader exportReader = new ExportedInterlinearReader(exportedXml, choices);
			ExportedParagraphValidator validator = new ExportedParagraphValidator(exportReader, para);
			validator.ValidateParagraphs(para, exportReader.GetParaNode(para.IndexInOwner));
		}


	}

	public class InterlinearExporterTests: InterlinearExporterTestsBase
	{
		[Test]
		public void AlternateCaseAnalyses_Baseline_LT5385()
		{
			CheckDisposed();

			m_choices.Add(InterlinLineChoices.kflidWord);
			XmlDocument exportedDoc = ExportToXml();
			IStTxtPara para0 = m_text1.ContentsOA.ParagraphsOS[0] as IStTxtPara;
			ValidateExportedParagraph(exportedDoc, m_choices, para0);
			// Set alternate case endings.
			ParagraphAnnotator ta = new ParagraphAnnotator(para0);
			string altCaseForm;
			ta.SetAlternateCase(0, 0, StringCaseStatus.allLower, out altCaseForm);
			Assert.AreEqual("xxxpus", altCaseForm);
			exportedDoc = this.ExportToXml();
			ValidateExportedParagraph(exportedDoc, m_choices, para0);
		}

		[Test]
		public void ExportVariantTypeInformation_LT9374()
		{
			CheckDisposed();

			m_choices.Add(InterlinLineChoices.kflidWord);
			m_choices.Add(InterlinLineChoices.kflidMorphemes);
			m_choices.Add(InterlinLineChoices.kflidLexEntries);
			m_choices.Add(InterlinLineChoices.kflidLexGloss);
			m_choices.Add(InterlinLineChoices.kflidLexPos);

			XmlDocument exportedDoc = ExportToXml();
			IStTxtPara para1 = m_text1.ContentsOA.ParagraphsOS[1] as IStTxtPara;
			ValidateExportedParagraph(exportedDoc, m_choices, para1);
			// Set alternate case endings.

			ParagraphAnnotator ta = new ParagraphAnnotator(para1);
			string formLexEntry = "go";
			ITsString tssLexEntryForm = TsStringUtils.MakeTss(formLexEntry, Cache.DefaultVernWs);
			int clsidForm;
			ILexEntry leGo = LexEntry.CreateEntry(Cache,
				MoMorphType.FindMorphType(Cache, new MoMorphTypeCollection(Cache), ref formLexEntry, out clsidForm), tssLexEntryForm,
				"go.pst", null);
			ta.SetVariantOf(0, 1, leGo, "fr. var.");
			exportedDoc = this.ExportToXml();
			ValidateExportedParagraph(exportedDoc, m_choices, para1);
		}


		[Test]
		public void ExportGuesses()
		{
			//NOTE: The new test paragraphs need to have all new words w/o duplicates so we can predict the guesses
			//xxxcrayzee xxxyouneek xxxsintents.

			// copy a text of first paragraph into a new paragraph to generate guesses.
			StTxtPara paraGlossed = m_text1.ContentsOA.ParagraphsOS.Append(new StTxtPara()) as StTxtPara;
			StTxtPara paraGuessed = m_text1.ContentsOA.ParagraphsOS.Append(new StTxtPara()) as StTxtPara;
			paraGlossed.Contents.UnderlyingTsString = TsStringUtils.MakeTss("xxxcrayzee xxxyouneek xxxsintents.", Cache.DefaultVernWs);
			paraGuessed.Contents.UnderlyingTsString = paraGlossed.Contents.UnderlyingTsString;

			// collect expected guesses from the glosses in the first paragraph.
			ParagraphAnnotator paGlossed = new ParagraphAnnotator(paraGlossed);
#pragma warning disable 219
			List<int> expectedGuesses = paGlossed.SetupDefaultWordGlosses();
#pragma warning restore 219

			// then verify we've created guesses for the new text.
			ParagraphAnnotator paGuessed = new ParagraphAnnotator(paraGuessed);
			bool fDidParse;
			ParagraphParser.ParseText(m_text1.ContentsOA, new NullProgressState(), out fDidParse);
			paGuessed.LoadParaDefaultAnalyses();

			// export the paragraph and test the Display results
			m_choices.Add(InterlinLineChoices.kflidWord);
			m_choices.Add(InterlinLineChoices.kflidWordGloss);
			XmlDocument exportedDoc = ExportToXml();
			ValidateExportedParagraph(exportedDoc, m_choices, paraGuessed);
		}

#if GoThroughThePainOfMaintainingThese
		// Note: Andy Black has converted these on his machine to regression tests run by batch files using his favorite difference tools, etc.
		// Note: Maintaining these as they exist here is quite time consuming.  The batch-oriented regression tests are not.

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// XLingPap word (not morpheme) aligned export transform, as examples
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void XLingPapExamplesWordAlignedTransform()
		{
			string sTransformPath = Path.Combine(SIL.Utils.DirectoryFinder.FWCodeDirectory,
				"Language Explorer/Export Templates/Interlinear");
			string sTransformFile = Path.Combine(sTransformPath, "xml2XLingPapExamplesConcatMorphemes.xsl");
			XslCompiledTransform trans = new XslCompiledTransform(false);
			trans.Load(sTransformFile);
			ApplyTransform("Phase1-OrizabaLesson2.xml",	"Phase1-OrizabaLesson2WordAlignedXLingPap.xml", trans);
			ApplyTransform("Phase1-KalabaTest.xml", "Phase1-KalabaTestWordAlignedXLingPap.xml", trans);
			ApplyTransform("Phase1-KalabaTestPunctuation.xml", "Phase1-KalabaTestPunctuationWordAlignedXLingPap.xml", trans);
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// XLingPap word (not morpheme) aligned export transform, as examples
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void XLingPapAppendixWordAlignedTransform()
		{
			string sTransformPath = Path.Combine(SIL.Utils.DirectoryFinder.FWCodeDirectory,
				"Language Explorer/Export Templates/Interlinear");
			string sTransformFile = Path.Combine(sTransformPath, "xml2XLingPapConcatMorphemes.xsl");
			XslCompiledTransform trans = new XslCompiledTransform(false);
			trans.Load(sTransformFile);
			ApplyTransform("Phase1-SETepehuanCorn.xml", "SETepehuanCornWordsAppendix.xml", trans);
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// XLingPap word (not morpheme) aligned export transform
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void XLingPapSingleExampleWordAlignedTransform()
		{
			string sTransformPath = Path.Combine(SIL.Utils.DirectoryFinder.FWCodeDirectory,
				"Language Explorer/Export Templates/Interlinear");
			string sTransformFile = Path.Combine(sTransformPath, "xml2XLingPapListExamplesConcatMorphemes.xsl");
			XslCompiledTransform trans = new XslCompiledTransform(false);
			trans.Load(sTransformFile);
			ApplyTransform("Phase1-SETepehuanCorn.xml", "SETepehuanCornSingleListExample.xml", trans);
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Applies the transform.
		/// </summary>
		/// <param name="sInputFile">The input file filename.</param>
		/// <param name="sExpectedOutput">The expected output filename.</param>
		/// <param name="transform">The transform.</param>
		/// ------------------------------------------------------------------------------------
		private void ApplyTransform(string sInputFile, string sExpectedOutput,
			XslCompiledTransform transform)
		{
			string sTestPath = Path.Combine(SIL.Utils.DirectoryFinder.FwSourceDirectory,
			"LexText/Interlinear/ITextDllTests/ExportTestFiles");
			string sInput = Path.Combine(sTestPath, sInputFile);
			XPathDocument doc = new XPathDocument(sInput);
			string sOutput = CreateTempFile("xml");
			StreamWriter result = null;
			try
			{
				result = new StreamWriter(sOutput);
				transform.Transform(doc, null, result);
				result.Close();
				result = null;
				string sExpectedResult = Path.Combine(sTestPath, sExpectedOutput);
				CheckXmlEquals(sExpectedResult, sOutput);
				// by deleting it here instead of at the finally spot below, when it fails, we can see what the result is.
				File.Delete(sOutput);
			}
			finally
			{
				if (result != null)
					result.Close();
#if Orig
				File.Delete(sOutput);
#endif
			}
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verfifies that the result XML file is identical to the expected XML file.
		/// </summary>
		/// <param name="sExpectedResultFile">The expected result filename.</param>
		/// <param name="sActualResultFile">The actual result filename.</param>
		/// ------------------------------------------------------------------------------------
		private void CheckXmlEquals(string sExpectedResultFile, string sActualResultFile)
		{
			string sActual = "";
			string sExpected = "";
			using (StreamReader actual = new StreamReader(sActualResultFile))
			{
				using (StreamReader expected = new StreamReader(sExpectedResultFile))
				{
					sExpected = expected.ReadToEnd();
				}
				sActual = actual.ReadToEnd();
			}
			StringBuilder sb = new StringBuilder();
			sb.Append("Expected file was ");
			sb.Append(sExpectedResultFile);
			Assert.AreEqual(sExpected, sActual, sb.ToString());
		}
#endif // GoThroughThePainOfMaintainingThese
	}

	public class InterlinearExporterTestsForELAN : InterlinearExporterTestsBase
	{
		protected override SIL.FieldWorks.FDO.IText SetupDataForText1()
		{
			FDO.IText text = Cache.LangProject.TextsOC.Add(new Text());
			text.ContentsOA = new StText();
			return text;
		}

		internal class ExportedParagraphValidatorForELAN : ExportedParagraphValidator
		{
			/// <summary>
			/// tracks that validated guids are also unique. key is guid and value is context.
			/// </summary>
			private IDictionary<string, string> m_nonrepeatingGuidsAndContext = new Dictionary<string, string>();

			internal ExportedParagraphValidatorForELAN(ExportedInterlinearReader reader, IStTxtPara para)
				: base(reader, para)
			{

			}

			protected override void ValidateParagraphOuterElements(object expectedParaInfo, object actualParaInfo, string paraContext)
			{
				// base override.
				string guidActual;
				ValidateCmObjectGuidForNode((expectedParaInfo as ICmObject).Hvo, actualParaInfo as XmlNode, paraContext, out guidActual);
				ValidateGuidIsNonrepeating(guidActual, paraContext);
				return;
			}

			private void ValidateGuidIsNonrepeating(string guidActual, string context)
			{
				string prevContext = "";
				Assert.IsFalse(m_nonrepeatingGuidsAndContext.TryGetValue(guidActual, out prevContext),
							   String.Format("Guid({0}) should be nonrepeatable, but was found these contexts:{3}{1},{3}{2}",
											 guidActual, prevContext, context, Environment.NewLine));
				m_nonrepeatingGuidsAndContext.Add(guidActual, context);
			}

			protected override void ValidateSegmentOuterElements(object expectedSegment, object actualSegment, string segmentContext)
			{
				// First validate the segment guid.
				string guidActual;
				ValidateCmObjectGuidForNode((int)expectedSegment, actualSegment as XmlNode, segmentContext, out guidActual);
				ValidateGuidIsNonrepeating(guidActual, segmentContext);
				base.ValidateSegmentOuterElements(expectedSegment, actualSegment, segmentContext);
			}

			protected override void ValidateSegFormOuterElements(object expectedSegForm, object actualSegForm, string segFormContext)
			{
				// First validate the twfic guid.
				ICmBaseAnnotation cbaExpected = CmBaseAnnotation.CreateFromDBObject(m_cache, (int)expectedSegForm);
				if (cbaExpected.AnnotationTypeRAHvo == PunctuationAnnotationType)
				{
					// we are not exporting punctuation guids, since some are dummys without guids.
					ValidateMissingGuidForNode(actualSegForm as XmlNode, segFormContext);
				}
				else
				{
					Assert.IsFalse(cbaExpected.IsDummyObject,
						String.Format("We expected our paragraph segform ({0}) in context {1} to be real.",
						cbaExpected.Hvo, segFormContext));
					string guidActual;
					ValidateCmObjectGuidForNode((int) expectedSegForm, actualSegForm as XmlNode, segFormContext,
												out guidActual);
					ValidateGuidIsNonrepeating(guidActual, segFormContext);
				}
				base.ValidateSegFormOuterElements(expectedSegForm, actualSegForm, segFormContext);
			}

			internal void ValidateNonrepeatingGuidCount(int expectedGuidCount)
			{
				Assert.AreEqual(expectedGuidCount, m_nonrepeatingGuidsAndContext.Count, "Expected a different number of nonrepeating guids");
			}
		}

		/// <summary>
		/// Create two paragraphs with two identical sentences. The first paragraph has real analyses, the second has only guesses.
		/// Validate that the guids for each paragraph, and each phrase and word annotation are unique.
		/// </summary>
		[Test]
		public void ExportPhraseWordGuids()
		{
			// create two paragraphs with two identical sentences.
			// copy a text of first paragraph into a new paragraph to generate guesses.
			StTxtPara paraGlossed = m_text1.ContentsOA.ParagraphsOS.Append(new StTxtPara()) as StTxtPara;
			StTxtPara paraGuessed = m_text1.ContentsOA.ParagraphsOS.Append(new StTxtPara()) as StTxtPara;
			paraGlossed.Contents.UnderlyingTsString = TsStringUtils.MakeTss(
				"xxxwordone xxxwordtwo xxxwordthree. xxxwordone xxxwordtwo xxxwordthree.",
				Cache.DefaultVernWs);
			paraGuessed.Contents.UnderlyingTsString = paraGlossed.Contents.UnderlyingTsString;

			// collect expected guesses from the glosses in the first paragraph.
			ParagraphAnnotator paGlossed = new ParagraphAnnotator(paraGlossed);
#pragma warning disable 219
			List<int> expectedGuesses = paGlossed.SetupDefaultWordGlosses();
#pragma warning restore 219

			// then verify we've created guesses for the new text.
			ParagraphAnnotator paGuessed = new ParagraphAnnotator(paraGuessed);
			bool fDidParse;
			ParagraphParser.ParseText(m_text1.ContentsOA, new NullProgressState(), out fDidParse);
			paGuessed.LoadParaDefaultAnalyses();

			// export the paragraph and test the Display results
			m_choices.Add(InterlinLineChoices.kflidWord);
			m_choices.Add(InterlinLineChoices.kflidWordGloss);
			m_choices.Add(InterlinLineChoices.kflidMorphemes);
			m_choices.Add(InterlinLineChoices.kflidLexEntries);
			m_choices.Add(InterlinLineChoices.kflidLexGloss);
			m_choices.Add(InterlinLineChoices.kflidLexPos);

			XmlDocument exportedDoc = ExportToXml("elan");
			// validate that we included the expected metadata
			string exportName = XmlUtils.GetOptionalAttributeValue(exportedDoc.DocumentElement, "exportTarget");
			Assert.AreEqual("elan", exportName);
			string version = XmlUtils.GetOptionalAttributeValue(exportedDoc.DocumentElement, "version");
			Assert.AreEqual("1", version);
			ExportedInterlinearReader exportReader = new ExportedInterlinearReader(exportedDoc, m_choices);
			ExportedParagraphValidatorForELAN validator = new ExportedParagraphValidatorForELAN(exportReader, paraGlossed);
			validator.ValidateParagraphs(paraGlossed, exportReader.GetParaNode(paraGlossed.IndexInOwner));
			validator.ValidateParagraphs(paraGuessed, exportReader.GetParaNode(paraGuessed.IndexInOwner));
			// only expecting to collect a total of 2 paragraph guids,
			// each paragraph with 2 phrase guids (2*2)
			// and each phrase with 3 word guids (2*2*3).
			validator.ValidateNonrepeatingGuidCount(2 + 2*2 + 2*2*3);
		}
	}

}
