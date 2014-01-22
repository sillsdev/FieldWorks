// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2014, SIL International. All Rights Reserved.
// <copyright from='2014' to='2014' company='SIL International'>
//		Copyright (c) 2014, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using PatrParserWrapper;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FDO.Validation;
using SIL.HermitCrab;
using SIL.Utils;

namespace SIL.FieldWorks.WordWorks.Parser
{
	public class HCParser : FwDisposableBase, IParser
	{
		private readonly FdoCache m_cache;
		private PatrParser m_patr;
		private readonly Loader m_loader;
		private M3ParserModelRetriever m_retriever;
		private readonly string m_dataDir;
		private readonly string m_outputDirectory;
		// m_projectName here is only used to create temporary files for the parser to load.
		// We convert the name to use strictly ANSI characters so that the patr parsers (which is
		// a legacy C program) can read the file names.
		private readonly string m_projectName;

		public HCParser(FdoCache cache, string dataDir)
		{
			m_cache = cache;
			m_dataDir = dataDir;

			m_retriever = new M3ParserModelRetriever(m_cache);
			m_patr = new PatrParser
			{
				CommentChar = '|',
				CodePage = Encoding.UTF8.CodePage
			};
			m_loader = new XmlLoader
			{
				XmlResolver = new XmlFwResolver(dataDir),
				QuitOnError = false
			};

			m_outputDirectory = Path.GetTempPath();
			m_projectName = ParserHelper.ConvertNameToUseAnsiCharacters(cache.ProjectId.Name);
		}

		#region IParser implementation
		public void Update()
		{
			CheckDisposed();

			if (!m_retriever.RetrieveModel())
				return;

			XmlDocument fxtResult = m_retriever.ModelDom;
			XmlDocument gafawsFxtResult = m_retriever.TemplateDom;
			LoadParser(ref fxtResult);
		}

		public void Reset()
		{
			CheckDisposed();

			m_retriever.Reset();
		}

		public ParseResult ParseWord(string word)
		{
			CheckDisposed();

			if (!m_loader.IsLoaded)
				return null;

			ICollection<WordSynthesis> synthesisRecs;
			try
			{
				synthesisRecs = m_loader.CurrentMorpher.MorphAndLookupWord(word);
			}
			catch (MorphException me)
			{
				return new ParseResult(ProcessMorphException(me));
			}

			ParseResult result;
			using (new WorkerThreadReadHelper(m_cache.ServiceLocator.GetInstance<IWorkerThreadReadHandler>()))
			{
				IEnumerable<PatrResult> patrResults = ProcessPatr(synthesisRecs, Path.Combine(m_outputDirectory, m_projectName + "patrlex.txt"), false);

				var analyses = new List<ParseAnalysis>();
				foreach (PatrResult patrResult in patrResults)
				{
					var morphs = new List<ParseMorph>();
					bool skip = false;
					foreach (PcPatrMorph pcPatrMorph in patrResult.Morphs)
					{
						ParseMorph morph;
						if (!ParserHelper.TryCreateParseMorph(m_cache, pcPatrMorph.formId, pcPatrMorph.msaId, out morph))
						{
							skip = true;
							break;
						}
						if (morph != null)
							morphs.Add(morph);
					}

					if (!skip && morphs.Count > 0)
						analyses.Add(new ParseAnalysis(morphs));
				}
				result = new ParseResult(analyses);
			}

			return result;
		}

		public string TraceWordXml(string form, string selectTraceMorphs)
		{
			CheckDisposed();

			return TraceWordXml(form, GetArrayFromString(selectTraceMorphs));
		}

		public string ParseWordXml(string form)
		{
			CheckDisposed();

			return ParseToXML(form, false, null);
		}
		#endregion

		public string TraceWordXml(string form, string[] selectTraceMorphs)
		{
			CheckDisposed();

			return ParseToXML(form, true, selectTraceMorphs);
		}

		protected override void DisposeManagedResources()
		{
			if (m_patr != null)
			{
				m_patr.Dispose();
				m_patr = null;
			}

			if (m_retriever != null)
			{
				m_retriever.Dispose();
				m_retriever = null;
			}
		}

		#region Load
		private void LoadParser(ref XmlDocument model)
		{
			string hcPath = HcInputPath;
			File.Delete(hcPath); // In case we don't produce one successfully, don't keep an old one.
			// Check for errors that will prevent the transformations working.
			using (new WorkerThreadReadHelper(m_cache.ServiceLocator.GetInstance<IWorkerThreadReadHandler>()))
			{
				foreach (var affix in m_cache.ServiceLocator.GetInstance<IMoAffixAllomorphRepository>().AllInstances())
				{
					string form = affix.Form.VernacularDefaultWritingSystem.Text;
					if (String.IsNullOrEmpty(form) || !form.Contains("["))
						continue;
					string environment = "/_" + form;
					// A form containing a reduplication expression should look like an environment
					var validator = new PhonEnvRecognizer(
						m_cache.LangProject.PhonologicalDataOA.AllPhonemes().ToArray(),
						m_cache.LangProject.PhonologicalDataOA.AllNaturalClassAbbrs().ToArray());
					if (!validator.Recognize(environment))
					{
						m_loader.Reset(); // make sure nothing thinks it is in a useful state
						throw new InvalidReduplicationEnvironmentException(validator.ErrorMessage, form);
					}
				}
			}

			var transformer = new M3ToHCTransformer(m_projectName, m_dataDir);
			transformer.MakeHCFiles(ref model);

			m_patr.LoadGrammarFile(HcGrammarPath);

			LoadHCInfo(hcPath);

			XmlNode delReappsNode = model.SelectSingleNode("/M3Dump/ParserParameters/HC/DelReapps");
			if (delReappsNode != null)
				m_loader.CurrentMorpher.DelReapplications = Convert.ToInt32(delReappsNode.InnerText);
		}

		private void LoadHCInfo(string hcPath)
		{
			string loadErrorsFile = Path.Combine(m_outputDirectory, m_projectName + "HCLoadErrors.xml");
			using (XmlWriter writer = new XmlTextWriter(loadErrorsFile, null))
			{
				var loadOutput = new XmlOutput(writer);
				writer.WriteStartElement("LoadErrors");
				m_loader.Output = loadOutput;
				m_loader.Load(hcPath);
				writer.WriteEndElement();
				loadOutput.Close();
			}
		}

		private string HcInputPath
		{
			get { return Path.Combine(m_outputDirectory, m_projectName + "HCInput.xml"); }
		}

		private string HcGrammarPath
		{
			get { return Path.Combine(m_outputDirectory, m_projectName + "gram.txt"); }
		}
		#endregion

		private string[] GetArrayFromString(string selectTraceMorphs)
		{
			string[] selectTraceMorphIds = null;
			if (!String.IsNullOrEmpty(selectTraceMorphs))
			{
				selectTraceMorphIds = selectTraceMorphs.TrimEnd().Split(' ');
				int i = 0;
				using (new WorkerThreadReadHelper(m_cache.ServiceLocator.GetInstance<IWorkerThreadReadHandler>()))
				{
					foreach (string sId in selectTraceMorphIds)
					{
						IMoMorphSynAnalysis msa = m_cache.ServiceLocator.GetInstance<IMoMorphSynAnalysisRepository>().GetObject(int.Parse(sId));
						if (msa.ClassID == MoStemMsaTags.kClassId)
							selectTraceMorphIds.SetValue("lex" + sId, i);
						else
							selectTraceMorphIds.SetValue("mrule" + sId, i);
						i++;
					}
				}
			}
			return selectTraceMorphIds;
		}

		private string ParseToXML(string form, bool trace, string[] selectTraceMorphs)
		{
			if (!m_loader.IsLoaded)
				return ParserCoreStrings.ksDidNotParse;

			var sb = new StringBuilder();
			var settings = new XmlWriterSettings { OmitXmlDeclaration = true };

			using (new WorkerThreadReadHelper(m_cache.ServiceLocator.GetInstance<IWorkerThreadReadHandler>()))
			{
				using (var writer = XmlWriter.Create(sb, settings))
				{
					writer.WriteStartElement("Wordform");
					writer.WriteAttributeString("DbRef", Convert.ToString(0));
					writer.WriteAttributeString("Form", form);
					var output = new FwXmlOutput(this, writer, trace,
												Path.Combine(m_outputDirectory, m_projectName + "patrlex.txt"));
					output.MorphAndLookupWord(m_loader.CurrentMorpher, form, true, selectTraceMorphs);
					writer.WriteEndElement();
				}
			}
			return sb.ToString();
		}

		private IEnumerable<PatrResult> ProcessPatr(IEnumerable<WordSynthesis> synthesisRecs, string patrlexPath, bool trace)
		{
			IList<PatrResult> patrResults = new List<PatrResult>();
			bool passedPatr = false;
			foreach (WordSynthesis ws in synthesisRecs)
			{
				WordGrammarTrace wordGrammarTrace = null;
				List<PcPatrMorph> morphs = GetMorphs(ws);
				if (trace)
					wordGrammarTrace = new WordGrammarTrace(((uint)ws.GetHashCode()).ToString(CultureInfo.InvariantCulture), morphs, m_cache);
				if (morphs.Count == 1)
				{
					PcPatrMorph morph = morphs[0];
					string formid = morph.formId;
					IMoForm form = m_cache.ServiceLocator.GetInstance<IMoFormRepository>().GetObject(Int32.Parse(formid));
					var morphtype = form.MorphTypeRA;
					if (morphtype.IsBoundType)
					{
						if (wordGrammarTrace != null)
							wordGrammarTrace.Success = false; // this is not really true; what other options are there?
						continue;
					}
				}
				WritePcPatrLexiconFile(patrlexPath, morphs);
				m_patr.LoadLexiconFile(patrlexPath, 0);
				string sentence = BuildPcPatrInputSentence(morphs);
				try
				{
					if (m_patr.ParseString(sentence) != null)
					{
						passedPatr = true;
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
				patrResults.Add(new PatrResult { Morphs = morphs, WordGrammarTrace = wordGrammarTrace, PassedPatr = passedPatr });
			}
			return patrResults;
		}

		private static List<PcPatrMorph> GetMorphs(WordSynthesis ws)
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
					int wordTypeIndex = WordTypeIndex(oldMorph.formIndex + 1, wordTypes.Count());
					ppMorph = new PcPatrMorph
					{
						formIndex = oldMorph.formIndex + 1,
						formId = formIds[oldMorph.formIndex + 1],
						form = form,
						wordType = wordTypes[wordTypeIndex]
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

		private static void WritePcPatrLexiconFile(string path, IEnumerable<PcPatrMorph> morphs)
		{
			using (var writer = new StreamWriter(path))
			{
				foreach (PcPatrMorph morph in morphs)
				{
					writer.WriteLine("\\w {0}", morph.Form);
					writer.WriteLine("\\c {0}", morph.wordType);
					writer.WriteLine("\\g {0}", morph.gloss);
					if (!String.IsNullOrEmpty(morph.featureDescriptors))
					{
						string lastFeatDesc = "";
						string combinedCFPFeatDescs = "";
						string[] featDescs = morph.featureDescriptors.Split(' ');
						if (featDescs.Any())
							writer.Write("\\f");
						foreach (string featDesc in featDescs)
						{
							if (featDesc.StartsWith("CFP"))
							{
								combinedCFPFeatDescs += featDesc;
								lastFeatDesc = featDesc;
								continue;
							}
							if (lastFeatDesc.StartsWith("CFP"))
								writer.Write(" {0}", combinedCFPFeatDescs);
							writer.Write(" {0}", featDesc);
						}
						if (lastFeatDesc.StartsWith("CFP"))
							writer.Write(" {0}", combinedCFPFeatDescs);
					}
					writer.WriteLine();
					writer.WriteLine();
				}
				writer.Close();
			}
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

		private static int WordTypeIndex(int expectedindex, int wordTypesCount)
		{
			int wordTypeIndex = expectedindex;
			if (wordTypeIndex + 1 > wordTypesCount)
			{
				// something is wrong (perhaps a missing suffix slot for a circumfix)
				// we'll use the same as last time, hoping for a parse failure
				wordTypeIndex = wordTypeIndex - 1;
			}
			return wordTypeIndex;
		}

		private string ProcessMorphException(MorphException me)
		{
			string errorMessage;
			switch (me.ErrorType)
			{
				case MorphException.MorphErrorType.INVALID_SHAPE:
					var shape = (string)me.Data["shape"];
					var position = (int)me.Data["position"];
					var phonemesFoundSoFar = (string)me.Data["phonemesFoundSoFar"];
					string rest = shape.Substring(position);
					string restToUse = rest;
					LgGeneralCharCategory cc = m_cache.ServiceLocator.UnicodeCharProps.get_GeneralCategory(rest[0]);
					if (cc == LgGeneralCharCategory.kccMn)
					{
						// the first character is a diacritic, combining type of character
						// insert a space so it does not show on top of a single quote in the message string
						restToUse = " " + rest;
					}
					errorMessage = String.Format(ParserCoreStrings.ksHCInvalidWordform, shape, position + 1, restToUse, phonemesFoundSoFar);
					break;

				case MorphException.MorphErrorType.UNINSTANTIATED_FEATURE:
					var featId = me.Data["feature"] as string;
					var feat = me.Morpher.PhoneticFeatureSystem.GetFeature(featId);
					errorMessage = String.Format(ParserCoreStrings.ksHCUninstFeature, feat.Description);
					break;

				default:
					errorMessage = String.Format(ParserCoreStrings.ksHCDefaultErrorMsg, me.Message);
					break;
			}
			return errorMessage;
		}

		private class PatrResult
		{
			public List<PcPatrMorph> Morphs { get; set; }
			public WordGrammarTrace WordGrammarTrace { get; set; }
			public bool PassedPatr { get; set; }
		}

		#region class FwXmlOutput
		[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
			Justification = "m_parser is a reference to the parent class")]
		private class FwXmlOutput : XmlOutput
		{
			private readonly HCParser m_parser;
			private readonly bool m_fDotrace;
			private readonly string m_patrlexPath;

			public FwXmlOutput(HCParser parser, XmlWriter writer, bool fDotrace, string patrlexPath)
				: base(writer, new FwXmlTraceManager())
			{
				m_parser = parser;
				m_fDotrace = fDotrace;
				m_patrlexPath = patrlexPath;
			}

			public override void MorphAndLookupWord(Morpher morpher, string word, bool prettyPrint, bool printTraceInputs)
			{
				MorphAndLookupWord(morpher, word, printTraceInputs, null);
			}

			public void MorphAndLookupWord(Morpher morpher, string word, bool printTraceInputs, string[] selectTraceMorphs)
			{
				try
				{
					ICollection<WordGrammarTrace> wordGrammarTraces = null;
					if (m_fDotrace)
						wordGrammarTraces = new HashSet<WordGrammarTrace>();
					XmlTraceManager.WriteInputs = printTraceInputs;
					TraceManager.TraceAll = m_fDotrace;
					ICollection<WordSynthesis> synthesisRecs = morpher.MorphAndLookupWord(word, TraceManager, selectTraceMorphs);

					IEnumerable<PatrResult> patrResults = m_parser.ProcessPatr(synthesisRecs, m_patrlexPath, m_fDotrace);
					foreach (PatrResult patrResult in patrResults)
					{
						if (patrResult.PassedPatr)
							BuildXmlOutput(patrResult.Morphs);
						if (wordGrammarTraces != null)
							wordGrammarTraces.Add(patrResult.WordGrammarTrace);
					}

					if (m_fDotrace)
					{
						WriteTrace();
						ConvertWordGrammarTraceToXml(wordGrammarTraces);
					}
					XmlTraceManager.Reset();
				}
				catch (MorphException exc)
				{
					Write(exc);
				}
			}

			private void BuildXmlOutput(IEnumerable<PcPatrMorph> morphs)
			{
				XmlWriter.WriteStartElement("WfiAnalysis");
				XmlWriter.WriteStartElement("Morphs");

				foreach (PcPatrMorph morph in morphs)
				{
					XmlWriter.WriteStartElement("Morph");

					XmlWriter.WriteStartElement("MoForm");
					XmlWriter.WriteAttributeString("DbRef", morph.formId);
					XmlWriter.WriteAttributeString("Label", morph.form);
					XmlWriter.WriteAttributeString("wordType", morph.wordType);
					XmlWriter.WriteEndElement();

					XmlWriter.WriteStartElement("MSI");
					XmlWriter.WriteAttributeString("DbRef", morph.msaId);
					XmlWriter.WriteEndElement();

					XmlWriter.WriteEndElement();
				}

				XmlWriter.WriteEndElement();
				XmlWriter.WriteEndElement();
			}

			private void ConvertWordGrammarTraceToXml(IEnumerable<WordGrammarTrace> wordGrammarTraces)
			{
				XmlWriter.WriteStartElement("WordGrammarTrace");
				foreach (WordGrammarTrace trace in wordGrammarTraces)
				{
					trace.ToXml(XmlWriter);
				}
				XmlWriter.WriteEndElement();
			}

			public override void Write(LoadException le)
			{
				XmlWriter.WriteStartElement("Error");
				switch (le.ErrorType)
				{
					case LoadException.LoadErrorType.INVALID_ENTRY_SHAPE:
						var entryShape = le.Data["shape"] as string;
						var entryId = le.Data["entry"] as string;
						LexEntry entry = le.Loader.CurrentMorpher.Lexicon.GetEntry(entryId);
						XmlWriter.WriteString(String.Format(ParserCoreStrings.ksHCInvalidEntryShape, entryShape, entry.Description));
						break;

					case LoadException.LoadErrorType.INVALID_RULE_SHAPE:
						var ruleShape = le.Data["shape"] as string;
						var ruleId = le.Data["rule"] as string;
						MorphologicalRule rule = le.Loader.CurrentMorpher.GetMorphologicalRule(ruleId);
						XmlWriter.WriteString(String.Format(ParserCoreStrings.ksHCInvalidRuleShape, ruleShape, rule.Description));
						break;

					default:
						XmlWriter.WriteString(String.Format(ParserCoreStrings.ksHCDefaultErrorMsg, le.Message));
						break;
				}
				XmlWriter.WriteEndElement();
			}

			public override void Write(MorphException me)
			{
				XmlWriter.WriteStartElement("Error");
				XmlWriter.WriteString(m_parser.ProcessMorphException(me));
				XmlWriter.WriteEndElement();
			}
		}
		#endregion

		#region class WordGrammarTrace
		[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
			Justification = "m_cache is a reference and disposed in the parent class")]
		private class WordGrammarTrace
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
				set { m_fSuccess = value; }

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
		#endregion

		#region class FwXmlTraceManager
		private class FwXmlTraceManager : XmlTraceManager
		{
			public override void ReportSuccess(WordSynthesis output)
			{
				if (TraceSuccess)
				{
					XElement wsElem = Write("Result", output);
					wsElem.Add(new XAttribute("id", ((uint)output.GetHashCode()).ToString(CultureInfo.InvariantCulture)));
					((XElement)output.CurrentTraceObject).Add(new XElement("ReportSuccessTrace", wsElem));
				}
			}

			public override void MorphologicalRuleNotUnapplied(MorphologicalRule rule, WordAnalysis input)
			{
			}

			public override void MorphologicalRuleNotApplied(MorphologicalRule rule, WordSynthesis input)
			{
			}

			protected override XElement Write(string name, Allomorph allomorph)
			{
				XElement elem = Write(name, (HCObject)allomorph);

				string formIdsStr = allomorph.GetProperty("FormID");
				string msaId = allomorph.GetProperty("MsaID");
				if (!String.IsNullOrEmpty(formIdsStr) || !String.IsNullOrEmpty(msaId))
				{
					var morphElem = new XElement("Morph");

					if (!String.IsNullOrEmpty(formIdsStr))
					{
						string[] formIds = formIdsStr.Split(' ');
						string[] wordTypes = allomorph.GetProperty("WordCategory").Split(' ');
						for (int i = 0; i < formIds.Length; i++)
						{
							int wordTypeIndex = WordTypeIndex(i, wordTypes.Count());
							morphElem.Add(new XElement("MoForm", new XAttribute("DbRef", formIds[i]), new XAttribute("wordType", wordTypes[wordTypeIndex])));
							string featDesc = allomorph.GetProperty("FeatureDescriptors");
							morphElem.Add(string.IsNullOrEmpty(featDesc) ? new XElement("props") : new XElement("props", featDesc));
						}
					}

					if (!String.IsNullOrEmpty(msaId))
						morphElem.Add(new XElement("MSI", new XAttribute("DbRef", msaId)));
					elem.Add(morphElem);
				}

				return elem;
			}
		}
		#endregion

		#region class PcPatrMorph
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
				{ return String.IsNullOrEmpty(form) ? "0" : form; }
			}
		}
		#endregion

		#region class XmlFwResolver
		class XmlFwResolver : XmlUrlResolver
		{
			readonly Uri m_baseUri;

			public XmlFwResolver(string dataDir)
			{
				m_baseUri = new Uri(dataDir + Path.DirectorySeparatorChar);
			}

			public override Uri ResolveUri(Uri baseUri, string relativeUri)
			{
				return base.ResolveUri(m_baseUri, relativeUri);
			}
		}
		#endregion
	}
}
