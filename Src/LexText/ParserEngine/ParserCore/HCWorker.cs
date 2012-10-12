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
// File: HCWorker.cs
// Responsibility: FLEx Team
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Xml;
using System.Collections.Generic;
using System.Text;
using System.IO;

using SIL.FieldWorks.FDO;
using SIL.Utils;
using SIL.HermitCrab;
using PatrParserWrapper;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.Validation;

namespace SIL.FieldWorks.WordWorks.Parser
{
	public class HCParserWorker : ParserWorker
	{
		class PcPatrMorph
		{
			public string formId;
			public string form;
			public string wordType;
			public string msaId;
			public string featureDescriptors;
			public string gloss;
			public int formIndex;

			public string Form
			{
				get
				{ return string.IsNullOrEmpty(form) ? "0" : form;}
			}
		}

		class XmlFwResolver : XmlUrlResolver
		{
			readonly Uri m_baseUri;

			public XmlFwResolver()
			{
				m_baseUri = new Uri(DirectoryFinder.FWCodeDirectory + Path.DirectorySeparatorChar);
			}

			public override Uri ResolveUri(Uri baseUri, string relativeUri)
			{
				return base.ResolveUri(m_baseUri, relativeUri);
			}
		}

		class WordGrammarTrace
		{
			private readonly IEnumerable<PcPatrMorph> m_morphs;
			private bool m_fSuccess;
			private readonly string m_id;
			private readonly FdoCache m_cache;

			public WordGrammarTrace(string id, IEnumerable<PcPatrMorph> morphs, FdoCache cache)
			{
				m_id = id;
				m_morphs = morphs;
				m_cache = cache;
			}
			/// <summary>
			/// Get/set success status of word grammar attempt
			/// </summary>
			public bool Success
			{
				set
				{
					m_fSuccess = value;
				}

			}

			/// <summary>
			/// Report trace information as XML
			/// </summary>
			/// <param name="writer"></param>
			public void ToXml(XmlWriter writer)
			{
				writer.WriteStartElement("WordGrammarAttempt");
				writer.WriteStartAttribute("success");
				writer.WriteValue(m_fSuccess ? "true" : "false");
				writer.WriteEndAttribute();
				writer.WriteStartElement("Id");
				writer.WriteValue(m_id);
				writer.WriteEndElement();

				string sType = "pfx"; // try to guess morph type based on word type
				foreach (PcPatrMorph morph in m_morphs)
				{
					writer.WriteStartElement("Morphs");
					string sWordType = morph.wordType;
					writer.WriteStartAttribute("wordType");
					writer.WriteValue(sWordType);
					writer.WriteEndAttribute();
					writer.WriteStartAttribute("type");
					if (sType == "pfx" &&
						sWordType == "root")
						sType = "root";
					else if (sType == "root" &&
						sWordType != "root")
						sType = "sfx";
					writer.WriteValue(sType);
					writer.WriteEndAttribute();
					writer.WriteStartAttribute("alloid");
					writer.WriteValue(morph.formId);
					writer.WriteEndAttribute();
					writer.WriteStartAttribute("morphname");
					writer.WriteValue(morph.msaId);
					writer.WriteEndAttribute();

					writer.WriteStartElement("alloform");
					writer.WriteValue(morph.form);
					writer.WriteEndElement(); // alloform
					//writer.WriteStartElement("Msa");
					//writer.WriteEndElement(); // Msa
					int hvoForm = Convert.ToInt32(morph.formId);
					var obj = m_cache.ServiceLocator.GetObject(hvoForm);
					var stemAllo = obj as IMoStemAllomorph;
					if (stemAllo != null)
					{
						var stemName = stemAllo.StemNameRA;
						if (stemName != null)
						{
							writer.WriteStartElement("stemName");
							writer.WriteStartAttribute("id");
							writer.WriteValue(stemName.Hvo);
							writer.WriteEndAttribute();
							writer.WriteValue(stemName.Name.BestAnalysisAlternative.Text);
							writer.WriteEndElement(); // stemName
						}
					}
					writer.WriteStartElement("gloss");
					writer.WriteValue(morph.gloss);
					writer.WriteEndElement(); // gloss
					writer.WriteStartElement("citationForm");
					var form = obj as IMoForm;
					if (form != null)
					{
						var entry = form.Owner as ILexEntry;
						if (entry != null)
							writer.WriteValue(entry.HeadWord.Text);
					}
					writer.WriteEndElement(); // citationForm
					writer.WriteEndElement(); // Morphs
				}
				writer.WriteEndElement();
			}

		}

		class FwXmlOutput : XmlOutput
		{
			readonly bool m_fDotrace;
			readonly PatrParser m_patr;
			readonly string m_patrlexPath;
			readonly FdoCache m_cache;

			public FwXmlOutput(XmlWriter writer, bool fDotrace, PatrParser patr, string patrlexPath, FdoCache cache)
				: base(writer)
			{
				m_fDotrace = fDotrace;
				m_patr = patr;
				m_patrlexPath = patrlexPath;
				m_cache = cache;
			}

			public override void MorphAndLookupWord(Morpher morpher, string word, bool prettyPrint, bool printTraceInputs)
			{
				try
				{
					ICollection<WordGrammarTrace> wordGrammarTraces = null;
					if (m_fDotrace)
						wordGrammarTraces = new HashSet<WordGrammarTrace>();
					morpher.TraceAll = m_fDotrace;
					WordAnalysisTrace trace;
					ICollection<WordSynthesis> synthesisRecs = morpher.MorphAndLookupWord(word, out trace);
					foreach (WordSynthesis ws in synthesisRecs)
					{
						WordGrammarTrace wordGrammarTrace = null;
						IEnumerable<PcPatrMorph> morphs = GetMorphs(ws);
						if (wordGrammarTraces != null)
						{
							wordGrammarTrace = new WordGrammarTrace(((uint)ws.GetHashCode()).ToString(), morphs, m_cache);
							wordGrammarTraces.Add(wordGrammarTrace);
						}

						WritePcPatrLexiconFile(m_patrlexPath, morphs);
						m_patr.LoadLexiconFile(m_patrlexPath, 0);
						string sentence = BuildPcPatrInputSentence(morphs);
						try
						{
							if (m_patr.ParseString(sentence) != null)
							{
								BuildXmlOutput(morphs);
								if (wordGrammarTrace != null)
									wordGrammarTrace.Success = true;
							}
							else if (wordGrammarTrace != null)
							{
								wordGrammarTrace.Success = false;
							}
						}
						catch (Exception)
						{
						}
					}
					if (m_fDotrace)
					{
						Write(trace, prettyPrint, printTraceInputs);
						ConvertWordGrammarTraceToXml(wordGrammarTraces);
					}
				}
				catch (MorphException exc)
				{
					Write(exc);
				}
			}

			protected override void Write(HermitCrab.Trace trace, bool printTraceInputs)
			{
				if (trace.Type == HermitCrab.Trace.TraceType.REPORT_SUCCESS)
				{
					var rsTrace = (ReportSuccessTrace) trace;
					m_xmlWriter.WriteStartElement(rsTrace.GetType().Name);
					m_xmlWriter.WriteStartElement("Result");
					m_xmlWriter.WriteAttributeString("id", ((uint)rsTrace.Output.GetHashCode()).ToString());
					m_xmlWriter.WriteString(rsTrace.Output.Stratum.CharacterDefinitionTable.ToString(rsTrace.Output.Shape,
						ModeType.SYNTHESIS, true));
					m_xmlWriter.WriteEndElement();
					m_xmlWriter.WriteEndElement();
				}
				else
				{
					base.Write(trace, printTraceInputs);
				}
			}

			protected override void Write(string localName, Allomorph allo)
			{
				m_xmlWriter.WriteStartElement(localName);
				m_xmlWriter.WriteAttributeString("id", allo.ID);
				m_xmlWriter.WriteElementString("Description", allo.Description);

				string formIdsStr = allo.GetProperty("FormID");
				string msaId = allo.GetProperty("MsaID");
				if (!string.IsNullOrEmpty(formIdsStr) || !string.IsNullOrEmpty(msaId))
				{
					m_xmlWriter.WriteStartElement("Morph");

					if (!string.IsNullOrEmpty(formIdsStr))
					{
						string[] formIds = formIdsStr.Split(' ');
						string[] wordTypes = allo.GetProperty("WordCategory").Split(' ');
						for (int i = 0; i < formIds.Length; i++)
						{
							m_xmlWriter.WriteStartElement("MoForm");
							m_xmlWriter.WriteAttributeString("DbRef", formIds[i]);
							m_xmlWriter.WriteAttributeString("wordType", wordTypes[i]);
							m_xmlWriter.WriteEndElement();
						}
					}

					if (!string.IsNullOrEmpty(msaId))
					{
						m_xmlWriter.WriteStartElement("MSI");
						m_xmlWriter.WriteAttributeString("DbRef", msaId);
						m_xmlWriter.WriteEndElement();
					}

					m_xmlWriter.WriteEndElement();
				}

				m_xmlWriter.WriteEndElement();
			}

			private static string BuildPcPatrInputSentence(IEnumerable<PcPatrMorph> morphs)
			{
				var sentence = new StringBuilder();
				bool firstItem = true;
				foreach (PcPatrMorph morph in morphs)
				{
					if (!firstItem)
						sentence.Append(" ");
					sentence.Append(morph.Form);
					firstItem = false;
				}
				return sentence.ToString();
			}

			void ConvertWordGrammarTraceToXml(IEnumerable<WordGrammarTrace> wordGrammarTraces)
			{
				m_xmlWriter.WriteStartElement("WordGrammarTrace");
				foreach (WordGrammarTrace trace in wordGrammarTraces)
				{
					trace.ToXml(m_xmlWriter);
				}
				m_xmlWriter.WriteEndElement();
			}

			static IEnumerable<PcPatrMorph> GetMorphs(WordSynthesis ws)
			{
				var ppMorphs = new Dictionary<string, PcPatrMorph>();
				var result = new List<PcPatrMorph>();
				foreach (Morph morph in ws.Morphs)
				{
					string[] formIds = morph.Allomorph.GetProperty("FormID").Split(' ');
					string[] wordTypes = morph.Allomorph.GetProperty("WordCategory").Split(' ');
					string form = ws.Stratum.CharacterDefinitionTable.ToString(morph.Shape, ModeType.SYNTHESIS, false);
					PcPatrMorph ppMorph;
					if (!ppMorphs.TryGetValue(morph.Allomorph.Morpheme.ID, out ppMorph))
					{
						ppMorph = new PcPatrMorph
									{
										formIndex = 0,
										formId = formIds[0],
										form = form,
										wordType = wordTypes[0]
									};
					}
					else if (formIds.Length == 1)
					{
						ppMorph.form += form;
						continue;
					}
					else
					{
						PcPatrMorph oldMorph = ppMorph;
						ppMorph = new PcPatrMorph
									{
										formIndex = oldMorph.formIndex + 1,
										formId = formIds[oldMorph.formIndex + 1],
										form = form,
										wordType = wordTypes[oldMorph.formIndex + 1]
									};
					}

					ppMorph.msaId = morph.Allomorph.GetProperty("MsaID");
					ppMorph.featureDescriptors = morph.Allomorph.GetProperty("FeatureDescriptors");
					ppMorph.gloss = morph.Allomorph.Morpheme.Gloss.Description;
					ppMorphs[morph.Allomorph.Morpheme.ID] = ppMorph;

					string morphType = morph.Allomorph.GetProperty("MorphType");
					switch (morphType)
					{
					case MoMorphTypeTags.kMorphInfix:
					case MoMorphTypeTags.kMorphInfixingInterfix:
							if (result.Count == 0)
								result.Add(ppMorph);
							else
								result.Insert(result.Count - 1, ppMorph);
							break;

						default:
							result.Add(ppMorph);
							break;
					}
				}
				return result;
			}

			static void WritePcPatrLexiconFile(string path, IEnumerable<PcPatrMorph> morphs)
			{
				using (var writer = new StreamWriter(path))
				{
				foreach (PcPatrMorph morph in morphs)
				{
					writer.WriteLine("\\w {0}", morph.Form);
					writer.WriteLine("\\c {0}", morph.wordType);
					writer.WriteLine("\\g {0}", morph.gloss);
					if (!string.IsNullOrEmpty(morph.featureDescriptors))
					{
						string lastFeatDesc = "";
						string combinedCFPFeatDescs = "";
						string[] featDescs = morph.featureDescriptors.Split(' ');
						foreach (string featDesc in featDescs)
						{
							if (featDesc.StartsWith("CFP"))
							{
								combinedCFPFeatDescs += featDesc;
								lastFeatDesc = featDesc;
								continue;
							}
							if (lastFeatDesc.StartsWith("CFP"))
								writer.WriteLine("\\f {0}", combinedCFPFeatDescs);
							writer.WriteLine("\\f {0}", featDesc);
						}
						if (lastFeatDesc.StartsWith("CFP"))
							writer.WriteLine("\\f {0}", combinedCFPFeatDescs);
					}
					writer.WriteLine();
				}
				writer.Close();
			}
			}

			void BuildXmlOutput(IEnumerable<PcPatrMorph> morphs)
			{
				m_xmlWriter.WriteStartElement("WfiAnalysis");
				m_xmlWriter.WriteStartElement("Morphs");

				foreach (PcPatrMorph morph in morphs)
				{
					m_xmlWriter.WriteStartElement("Morph");

					m_xmlWriter.WriteStartElement("MoForm");
					m_xmlWriter.WriteAttributeString("DbRef", morph.formId);
					m_xmlWriter.WriteAttributeString("Label", morph.form);
					m_xmlWriter.WriteAttributeString("wordType", morph.wordType);
					m_xmlWriter.WriteEndElement();

					m_xmlWriter.WriteStartElement("MSI");
					m_xmlWriter.WriteAttributeString("DbRef", morph.msaId);
					m_xmlWriter.WriteEndElement();

					m_xmlWriter.WriteEndElement();
				}

				m_xmlWriter.WriteEndElement();
				m_xmlWriter.WriteEndElement();
			}

			public override void Write(LoadException le)
			{
				m_xmlWriter.WriteStartElement("Error");
				switch (le.ErrorType)
				{
					case LoadException.LoadErrorType.INVALID_ENTRY_SHAPE:
						var entryShape = le.Data["shape"] as string;
						var entryId = le.Data["entry"] as string;
						LexEntry entry = le.Loader.CurrentMorpher.Lexicon.GetEntry(entryId);
						m_xmlWriter.WriteString(string.Format(ParserCoreStrings.ksHCInvalidEntryShape, entryShape, entry.Description));
						break;

					case LoadException.LoadErrorType.INVALID_RULE_SHAPE:
						var ruleShape = le.Data["shape"] as string;
						var ruleId = le.Data["rule"] as string;
						MorphologicalRule rule = le.Loader.CurrentMorpher.GetMorphologicalRule(ruleId);
						m_xmlWriter.WriteString(string.Format(ParserCoreStrings.ksHCInvalidRuleShape, ruleShape, rule.Description));
						break;

					default:
						m_xmlWriter.WriteString(string.Format(ParserCoreStrings.ksHCDefaultErrorMsg, le.Message));
						break;
				}
				m_xmlWriter.WriteEndElement();
			}

			public override void Write(MorphException me)
			{
				m_xmlWriter.WriteStartElement("Error");
				switch (me.ErrorType)
				{
					case MorphException.MorphErrorType.INVALID_SHAPE:
						var shape = me.Data["shape"] as string;
						m_xmlWriter.WriteString(string.Format(ParserCoreStrings.ksHCInvalidWordform, shape));
						break;

					case MorphException.MorphErrorType.UNINSTANTIATED_FEATURE:
						var featId = me.Data["feature"] as string;
						var feat = me.Morpher.PhoneticFeatureSystem.GetFeature(featId);
						m_xmlWriter.WriteString(string.Format(ParserCoreStrings.ksHCUninstFeature, feat.Description));
						break;

					default:
						m_xmlWriter.WriteString(string.Format(ParserCoreStrings.ksHCDefaultErrorMsg, me.Message));
						break;
				}
				m_xmlWriter.WriteEndElement();
			}
		}

		private readonly XmlLoader m_loader;
		private PatrParser m_patr;
		private readonly string m_outputDirectory;

		public HCParserWorker(FdoCache cache, Action<TaskReport> taskUpdateHandler, IdleQueue idleQueue)
			: base(cache, taskUpdateHandler, idleQueue,
			cache.ServiceLocator.GetInstance<ICmAgentRepository>().GetObject(CmAgentTags.kguidAgentHermitCrabParser))
		{
			m_outputDirectory = Path.GetTempPath();
			m_patr = new PatrParser
						{
							CommentChar = '|',
							CodePage = Encoding.UTF8.CodePage
						};
			m_loader = new XmlLoader
						{
							XmlResolver = new XmlFwResolver(),
							QuitOnError = false
						};
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && !IsDisposed)
			{
				if (m_patr != null)
				{
					m_patr.Dispose();
					m_patr = null;
				}
			}
			base.Dispose(disposing);
		}

		protected override string ParseWord(string form, int hvoWordform)
		{
			return ParseWordWithHermitCrab(form, hvoWordform, false);
		}

		protected override string TraceWord(string form, string selectTraceMorphs)
		{
			return ParseWordWithHermitCrab(form, 0, true);
		}

		private string ParseWordWithHermitCrab(string form, int hvoWordform, bool fDotrace)
		{
			if (!m_loader.IsLoaded)
				return ParserCoreStrings.ksDidNotParse;

			var sb = new StringBuilder();
			var settings = new XmlWriterSettings { OmitXmlDeclaration = true };
			using (var writer = XmlWriter.Create(sb, settings))
			{
				writer.WriteStartElement("Wordform");
				writer.WriteAttributeString("DbRef", Convert.ToString(hvoWordform));
				writer.WriteAttributeString("Form", form);
				var output = new FwXmlOutput(writer, fDotrace, m_patr,
					Path.Combine(m_outputDirectory, m_projectName + "patrlex.txt"), m_cache);
				output.MorphAndLookupWord(m_loader.CurrentMorpher, form, true, true);
				writer.WriteEndElement();
				writer.Close();
				return sb.ToString();
			}
		}

		public string HcInputPath
		{
			get { return Path.Combine(m_outputDirectory, m_projectName + "HCInput.xml"); }
		}

		public string HcGrammarPath
		{
			get { return Path.Combine(m_outputDirectory, m_projectName + "gram.txt"); }
		}

		protected override void LoadParser(ref XmlDocument model, XmlDocument template)
		{
			string hcPath = HcInputPath;
			File.Delete(hcPath); // In case we don't produce one successfully, don't keep an old one.
			// Check for errors that will prevent the transformations working.
			foreach (var affix in m_cache.ServiceLocator.GetInstance<IMoAffixAllomorphRepository>().AllInstances())
			{
				string form = affix.Form.VernacularDefaultWritingSystem.Text;
				if (string.IsNullOrEmpty(form) || !form.Contains("["))
					continue;
				string environment = "/_" + form;
				// A form containing a reduplication expression should look like an environment
				var validator = new PhonEnvRecognizer(
					m_cache.LangProject.PhonologicalDataOA.AllPhonemes().ToArray(),
					m_cache.LangProject.PhonologicalDataOA.AllNaturalClassAbbrs().ToArray());
				if (!validator.Recognize(environment))
				{
					string msg = string.Format(ParserCoreStrings.ksHermitCrabReduplicationProblem, form,
						validator.ErrorMessage);
					m_cache.ThreadHelper.Invoke(() => // We may be running in a background thread
						{
							MessageBox.Show(Form.ActiveForm, msg, ParserCoreStrings.ksBadAffixForm,
								MessageBoxButtons.OK, MessageBoxIcon.Error);
						});
					m_loader.Reset(); // make sure nothing thinks it is in a useful state
					return; // We can't load the parser, hopefully our caller will realize we failed.
				}
			}

			var transformer = new M3ToHCTransformer(m_projectName, m_taskUpdateHandler);
			transformer.MakeHCFiles(ref model);

			m_patr.LoadGrammarFile(HcGrammarPath);
			m_loader.Load(hcPath);

			XmlNode delReappsNode = model.SelectSingleNode("/M3Dump/ParserParameters/HC/DelReapps");
			if (delReappsNode != null)
				m_loader.CurrentMorpher.DelReapplications = Convert.ToInt32(delReappsNode.InnerText);
		}
	}
}
