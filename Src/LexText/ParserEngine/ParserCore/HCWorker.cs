using System;
using System.Diagnostics;
using System.Data.SqlClient;
using System.Xml;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.IO;

using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Common.Utils;

using SIL.HermitCrab;
using PcPatr;

namespace SIL.FieldWorks.WordWorks.Parser
{
	internal class HCParserWorker : ParserWorker
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
			Uri m_baseUri;

			public XmlFwResolver()
			{
				m_baseUri = new Uri(DirectoryFinder.FWCodeDirectory + "\\");
			}

			public override Uri ResolveUri(Uri baseUri, string relativeUri)
			{
				return base.ResolveUri(m_baseUri, relativeUri);
			}
		}

		class WordGrammarTrace
		{
			private IEnumerable<PcPatrMorph> m_morphs;
			private bool m_fSuccess;
			private string m_id;

			public WordGrammarTrace(string id, IEnumerable<PcPatrMorph> morphs)
			{
				m_id = id;
				m_morphs = morphs;
			}
			/// <summary>
			/// Get/set success status of word grammar attempt
			/// </summary>
			public bool Success
			{
				get
				{ return m_fSuccess; }
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
					writer.WriteEndElement(); // Form
					//writer.WriteStartElement("Msa");
					//writer.WriteEndElement(); // Msa
					writer.WriteStartElement("Gloss");
					writer.WriteValue(morph.gloss);
					writer.WriteEndElement(); // Gloss
					writer.WriteEndElement(); // Morphs
				}
				writer.WriteEndElement();
			}

		}

		class FwXmlOutput : XmlOutput
		{
			bool m_fDotrace;
			PatrParser m_patr;
			string m_patrlexPath;

			public FwXmlOutput(XmlWriter writer, bool fDotrace, PatrParser patr, string patrlexPath)
				: base(writer)
			{
				m_fDotrace = fDotrace;
				m_patr = patr;
				m_patrlexPath = patrlexPath;
			}

			public override void MorphAndLookupWord(Morpher morpher, string word, bool prettyPrint, bool printTraceInputs)
			{
				try
				{
					ICollection<WordGrammarTrace> wordGrammarTraces = null;
					if (m_fDotrace)
						wordGrammarTraces = new Set<WordGrammarTrace>();
					morpher.TraceAll = m_fDotrace;
					WordAnalysisTrace trace;
					ICollection<WordSynthesis> synthesisRecs = morpher.MorphAndLookupWord(word, out trace);
					foreach (WordSynthesis ws in synthesisRecs)
					{
						WordGrammarTrace wordGrammarTrace = null;
						IEnumerable<PcPatrMorph> morphs = GetMorphs(ws);
						if (m_fDotrace)
						{
							wordGrammarTrace = new WordGrammarTrace(((uint)ws.GetHashCode()).ToString(), morphs);
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
								if (m_fDotrace)
									wordGrammarTrace.Success = true;
							}
							else if (m_fDotrace)
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

			protected override void Write(SIL.HermitCrab.Trace trace, bool printTraceInputs)
			{
				if (trace.Type == SIL.HermitCrab.Trace.TraceType.REPORT_SUCCESS)
				{
					ReportSuccessTrace rsTrace = trace as ReportSuccessTrace;
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

			private string BuildPcPatrInputSentence(IEnumerable<PcPatrMorph> morphs)
			{
				StringBuilder sentence = new StringBuilder();
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

			void ConvertWordGrammarTraceToXml(ICollection<WordGrammarTrace> wordGrammarTraces)
			{
				m_xmlWriter.WriteStartElement("WordGrammarTrace");
				foreach (WordGrammarTrace trace in wordGrammarTraces)
				{
					trace.ToXml(m_xmlWriter);
				}
				m_xmlWriter.WriteEndElement();
			}

			IEnumerable<PcPatrMorph> GetMorphs(WordSynthesis ws)
			{
				Dictionary<string, PcPatrMorph> ppMorphs = new Dictionary<string, PcPatrMorph>();
				List<PcPatrMorph> result = new List<PcPatrMorph>();
				foreach (Morph morph in ws.Morphs)
				{
					string[] formIds = morph.Allomorph.GetProperty("FormID").Split(' ');
					string[] wordTypes = morph.Allomorph.GetProperty("WordCategory").Split(' ');
					string form = ws.Stratum.CharacterDefinitionTable.ToString(morph.Shape, ModeType.SYNTHESIS, false);
					PcPatrMorph ppMorph;
					if (!ppMorphs.TryGetValue(morph.Allomorph.Morpheme.ID, out ppMorph))
					{
						ppMorph = new PcPatrMorph();
						ppMorph.formIndex = 0;
						ppMorph.formId = formIds[ppMorph.formIndex];
						ppMorph.form = form;
						ppMorph.wordType = wordTypes[ppMorph.formIndex];
					}
					else if (formIds.Length == 1)
					{
						ppMorph.form += form;
						continue;
					}
					else
					{
						PcPatrMorph oldMorph = ppMorph;
						ppMorph = new PcPatrMorph();
						ppMorph.formIndex = oldMorph.formIndex + 1;
						ppMorph.formId = formIds[ppMorph.formIndex];
						ppMorph.form = form;
						ppMorph.wordType = wordTypes[ppMorph.formIndex];
					}

					ppMorph.msaId = morph.Allomorph.GetProperty("MsaID");
					ppMorph.featureDescriptors = morph.Allomorph.GetProperty("FeatureDescriptors");
					ppMorph.gloss = morph.Allomorph.Morpheme.Gloss.Description;
					ppMorphs[morph.Allomorph.Morpheme.ID] = ppMorph;

					string morphType = morph.Allomorph.GetProperty("MorphType");
					switch (morphType)
					{
						case MoMorphType.kguidMorphInfix:
						case MoMorphType.kguidMorphInfixingInterfix:
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

			void WritePcPatrLexiconFile(string path, IEnumerable<PcPatrMorph> morphs)
			{
				StreamWriter writer = new StreamWriter(path);
				foreach (PcPatrMorph morph in morphs)
				{
					writer.WriteLine("\\w {0}", morph.Form);
					writer.WriteLine("\\c {0}", morph.wordType);
					writer.WriteLine("\\g {0}", morph.gloss);
					if (!string.IsNullOrEmpty(morph.featureDescriptors))
					{
						string[] featDescs = morph.featureDescriptors.Split(' ');
						foreach (string featDesc in featDescs)
							writer.WriteLine("\\f {0}", featDesc);
					}
					writer.WriteLine();
				}
				writer.Close();
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
						string entryShape = le.Data["shape"] as string;
						string entryId = le.Data["entry"] as string;
						SIL.HermitCrab.LexEntry entry = le.Loader.CurrentMorpher.Lexicon.GetEntry(entryId);
						m_xmlWriter.WriteString(string.Format(ParserCoreStrings.ksHCInvalidEntryShape, entryShape, entry.Description));
						break;

					case LoadException.LoadErrorType.INVALID_RULE_SHAPE:
						string ruleShape = le.Data["shape"] as string;
						string ruleId = le.Data["rule"] as string;
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
						string shape = me.Data["shape"] as string;
						m_xmlWriter.WriteString(string.Format(ParserCoreStrings.ksHCInvalidWordform, shape));
						break;

					case MorphException.MorphErrorType.UNINSTANTIATED_FEATURE:
						string featId = me.Data["feature"] as string;
						Feature feat = me.Morpher.PhoneticFeatureSystem.GetFeature(featId);
						m_xmlWriter.WriteString(string.Format(ParserCoreStrings.ksHCUninstFeature, feat.Description));
						break;

					default:
						m_xmlWriter.WriteString(string.Format(ParserCoreStrings.ksHCDefaultErrorMsg, me.Message));
						break;
				}
				m_xmlWriter.WriteEndElement();
			}
		}

		private XmlLoader m_loader = null;
		private PatrParser m_patr = null;
		private string m_outputDirectory;

		public HCParserWorker(SqlConnection connection, string database, string LangProject, TaskUpdateEventHandler handler)
			: base(connection, database, LangProject, handler, "HCParser", "Normal")
		{
			m_outputDirectory = Path.GetTempPath();
		}

		internal override int ThreadId
		{
			get
			{
				CheckDisposed();
				return Thread.CurrentThread.ManagedThreadId;
			}
		}

		internal override void InitParser()
		{
			CheckDisposed();
			m_patr = new PatrParser();
			m_patr.CommentChar = '|';
			m_patr.CodePage = Encoding.UTF8.CodePage;
			m_loader = new XmlLoader();
			m_loader.XmlResolver = new XmlFwResolver();
			m_loader.QuitOnError = false;
		}

		protected override void CleanupParser()
		{
			if (m_loader != null)
			{
				System.Runtime.InteropServices.Marshal.ReleaseComObject(m_patr);
				m_loader.Reset();
				m_loader = null;
			}
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
			Debug.Assert(m_loader.IsLoaded, "It looks like the calling code forgot to load HC.NET");

			StringBuilder sb = new StringBuilder();
			XmlWriterSettings settings = new XmlWriterSettings();
			settings.OmitXmlDeclaration = true;
			XmlWriter writer = XmlWriter.Create(sb, settings);
			writer.WriteStartElement("Wordform");
			writer.WriteAttributeString("DbRef", Convert.ToString(hvoWordform));
			writer.WriteAttributeString("Form", form);
			FwXmlOutput output = new FwXmlOutput(writer, fDotrace, m_patr,
				Path.Combine(m_outputDirectory, m_database + "patrlex.txt"));
			output.MorphAndLookupWord(m_loader.CurrentMorpher, form, true, true);
			writer.WriteEndElement();
			writer.Close();
			return sb.ToString();
		}

		protected override void LoadParser(ref XmlDocument model, XmlDocument template, TaskReport task, ParserScheduler.NeedsUpdate eNeedsUpdate)
		{
			try
			{
				M3ToHCTransformer transformer = new M3ToHCTransformer(m_database);
				transformer.MakeHCFiles(ref model, task, eNeedsUpdate);
			}
			catch (Exception error)
			{
				if (error.GetType() == Type.GetType("System.Threading.ThreadInterruptedException") ||
					error.GetType() == Type.GetType("System.Threading.ThreadAbortException"))
				{
					throw error;
				}

				task.EncounteredError(null);	// Don't want to show message box in addition to yellow crash box!
				throw new ApplicationException("Error while generating files for the Parser.", error);
			}

			try
			{
				string gramPath = Path.Combine(m_outputDirectory, m_database + "gram.txt");
				m_patr.LoadGrammarFile(gramPath);
				string hcPath = Path.Combine(m_outputDirectory, m_database + "HCInput.xml");
				m_loader.Load(hcPath);

				XmlNode delReappsNode = model.SelectSingleNode("/M3Dump/ParserParameters/HC/DelReapps");
				if (delReappsNode != null)
					m_loader.CurrentMorpher.DelReapplications = Convert.ToInt32(delReappsNode.InnerText);
			}
			catch (Exception error)
			{
				if (error.GetType() == Type.GetType("System.Threading.ThreadInterruptedException") ||
					error.GetType() == Type.GetType("System.Threading.ThreadAbortException"))
				{
					throw error;
				}
				ApplicationException e = new ApplicationException("Error while loading the Parser.", error);
				task.EncounteredError(null);	// Don't want to show message box in addition to yellow crash box!
				throw e;
			}
		}
	}
}
