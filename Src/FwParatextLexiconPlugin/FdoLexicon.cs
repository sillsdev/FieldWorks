// Copyright (c) 2015-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Web;
using Paratext.LexicalContracts;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel;
using SIL.LCModel.DomainImpl;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using SIL.LCModel.Utils;
using SIL.FieldWorks.WordWorks.Parser;
using SIL.Machine.Morphology;
using SIL.ObjectModel;
using SIL.PlatformUtilities;
using WordAnalysis = Paratext.LexicalContracts.WordAnalysis;

namespace SIL.FieldWorks.ParatextLexiconPlugin
{
	internal class FdoLexicon : DisposableBase, Lexicon, WordAnalyses, IVwNotifyChange, LexiconV2, WordAnalysesV2
	{
		internal const string AddedByParatext = "Added by Paratext";
		private IParser m_parser;
		private readonly LcmCache m_cache;
		private readonly string m_scrTextName;
		private readonly ConditionalWeakTable<ILexEntry, HomographNumber> m_homographNumbers;
		private Dictionary<LexemeKey, SortedSet<ILexEntry>> m_entryIndex;
		private readonly LexEntryComparer m_entryComparer;
		private readonly int m_defaultVernWs;
		private PoorMansStemmer<string, char> m_stemmer;
		private readonly string m_projectId;

		internal FdoLexicon(string scrTextName, string projectId, LcmCache cache, int defaultVernWs)
		{
			m_scrTextName = scrTextName;
			m_projectId = projectId;
			m_cache = cache;
			m_homographNumbers = new ConditionalWeakTable<ILexEntry, HomographNumber>();
			m_cache.DomainDataByFlid.AddNotification(this);
			m_entryComparer = new LexEntryComparer(this);
			m_defaultVernWs = defaultVernWs;
		}

		internal LcmCache Cache
		{
			get { return m_cache; }
		}

		internal string ProjectId
		{
			get { return m_projectId; }
		}

		internal string ScrTextName
		{
			get { return m_scrTextName; }
		}

		internal bool UpdatingEntries { get; set; }

		internal int DefaultVernWs
		{
			get { return m_defaultVernWs; }
		}

		protected override void DisposeManagedResources()
		{
			if (m_parser != null)
			{
				m_parser.Dispose();
				m_parser = null;
			}
		}

		#region Lexicon implementation

		public event LexemeAddedEventHandler LexemeAdded;

		public event LexiconSenseAddedEventHandler LexiconSenseAdded;

		public event LexiconGlossAddedEventHandler LexiconGlossAdded;

		public bool RequiresLanguageId
		{
			get { return true; }
		}

		public IEnumerable<Lexeme> Lexemes
		{
			get
			{
				var lexemes = new List<Lexeme>();
				// Get all of the lexical entries in the database
				foreach (ILexEntry entry in m_cache.ServiceLocator.GetInstance<ILexEntryRepository>().AllInstances())
					lexemes.Add(GetEntryLexeme(entry));

				// Get all the wordforms in the database
				foreach (IWfiWordform wordform in m_cache.ServiceLocator.GetInstance<IWfiWordformRepository>().AllInstances())
				{
					string wordFormWs = wordform.Form.get_String(m_defaultVernWs).Text;
					if (wordFormWs != null)
						lexemes.Add(new FdoWordformLexeme(this, new LexemeKey(LexemeType.Word, wordFormWs.Normalize())));
				}
				return lexemes;
			}
		}
	  /// <summary>
	  /// Version 1 of the api uses a type:wordform identifier but version 2 should supply a guid
	  /// </summary>
	  public Lexeme this[string id]
		{
			get
			{
				// Version 2
				if (Guid.TryParse(id, out var guid))
				{
					var entryOrAnalysis = Cache.ServiceLocator.ObjectRepository.GetObject(guid);
					if (entryOrAnalysis is ILexEntry entry)
					{
						return new FdoLexEntryLexeme(this, new LexemeKey(GetLexemeTypeForMorphType(entry.MorphTypes.First()), guid, Cache));
					}

					if (entryOrAnalysis is IWfiAnalysis analysis)
					{
						return new FdoWordformLexeme(this, new LexemeKey(LexemeType.Word, guid, Cache));
					}

				}

				// version 1
				return TryGetLexeme(new LexemeKey(id), out var lexeme) ? lexeme : null;
			}
		}

		private bool TryGetLexeme(LexemeKey key, out Lexeme lexeme)
		{
			if (key.Type == LexemeType.Word)
			{
				IWfiWordform wf;
				if (TryGetWordform(key, out wf))
				{
					lexeme = new FdoWordformLexeme(this, key);
					return true;
				}
			}
			else
			{
				ILexEntry entry;
				if (TryGetEntry(key, out entry))
				{
					lexeme = new FdoLexEntryLexeme(this, key);
					return true;
				}
			}

			lexeme = null;
			return false;
		}

		public Lexeme FindOrCreateLexeme(LexemeType type, string lexicalForm)
		{
			Lexeme lexeme;
			if (!TryGetLexeme(new LexemeKey(type, lexicalForm), out lexeme))
				lexeme = CreateLexeme(type, lexicalForm);
			return lexeme;
		}

		public Lexeme CreateLexeme(LexemeType type, string lexicalForm)
		{
			if (type == LexemeType.Word)
				return new FdoWordformLexeme(this, new LexemeKey(type, lexicalForm));

			var nexHomographNum = 1;
			nexHomographNum += GetMatchingEntries(new LexemeKey(type, lexicalForm)).Count();

			return new FdoLexEntryLexeme(this, new LexemeKey(type, lexicalForm, nexHomographNum));
		}

		public void RemoveLexeme(Lexeme lexeme)
		{
			if (lexeme.Type == LexemeType.Word)
			{
				IWfiWordform wordform;
				if (TryGetWordform(new LexemeKey(LexemeType.Word, lexeme.LexicalForm), out wordform))
					NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () => wordform.Delete());
			}
			else
			{
				var entryLexeme = (FdoLexEntryLexeme)lexeme;
				ILexEntry entry;
				if (TryGetEntry(entryLexeme.Key, out entry))
				{
					var key = new LexemeKey(lexeme.Type, lexeme.LexicalForm);
					SortedSet<ILexEntry> entries = m_entryIndex[key];
					entries.Remove(entry);
					UpdatingEntries = true;
					try
					{
						NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () => entry.Delete());
					}
					finally
					{
						UpdatingEntries = false;
					}
				}
			}
		}

		public void AddLexeme(Lexeme lexeme)
		{
			if (this[lexeme.Id] != null)
				throw new ArgumentException("The specified lexeme has already been added.", "lexeme");
			if (lexeme.Type == LexemeType.Word)
			{
				NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () => CreateWordform(lexeme.LexicalForm));
			}
			else if(lexeme is FdoLexEntryLexeme entryLexeme)
			{
				UpdatingEntries = true;
				try
				{
					NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () => CreateEntry(entryLexeme.Key));
				}
				finally
				{
					UpdatingEntries = false;
				}
			}
			else
			{
				throw new ArgumentException("The current code will only handle Lexemes created by FLEx (FdoLexEntryLexemes).");
			}
			OnLexemeAdded(lexeme);
		}

		public void Save()
		{
			m_cache.ServiceLocator.GetInstance<IUndoStackManager>().Save();
		}

		public Lexeme FindClosestMatchingLexeme(string wordForm)
		{
			wordForm = wordForm.Normalize(NormalizationForm.FormD);
			ITsString tss = TsStringUtils.MakeString(wordForm, DefaultVernWs);
			ILexEntry matchingEntry = m_cache.ServiceLocator.GetInstance<ILexEntryRepository>().FindEntryForWordform(m_cache, tss);

			if (matchingEntry == null)
				matchingEntry = GetMatchingEntryFromParser(wordForm);

			if (matchingEntry == null)
				matchingEntry = GetMatchingEntryFromStemmer(wordForm);

			if (matchingEntry == null)
				return null;

			return GetEntryLexeme(matchingEntry);
		}

		public IEnumerable<Lexeme> FindMatchingLexemes(string wordForm)
		{
			bool duplicates = false;
			var entriesForWordForm = m_cache.ServiceLocator.GetInstance<ILexEntryRepository>()
				.FindEntriesForWordform(m_cache,
					TsStringUtils.MakeString(wordForm.Normalize(NormalizationForm.FormD),
						DefaultVernWs), null, ref duplicates);
			return entriesForWordForm.Select(GetEntryLexeme).ToArray();
		}

		public bool CanOpenInLexicon
		{
			get
			{
				return FdoLexicon.IsFwInstalled;
			}
		}

		private static bool IsFwInstalled
		{
			get
			{
				return !string.IsNullOrEmpty(FwDir);
			}
		}

		private static string FwDir
		{
			get
			{
				string fwCodePath = Environment.GetEnvironmentVariable("FIELDWORKSDIR");
				if (!string.IsNullOrEmpty(fwCodePath) && Directory.Exists(fwCodePath))
				{
					return fwCodePath;
				}

				return null;
			}
		}

		public void OpenInLexicon(Lexeme lexeme)
		{
			string guid = null, toolName;
			if (lexeme.Type == LexemeType.Word)
			{
				toolName = "Analyses";
				IWfiWordform wf;
				if (TryGetWordform(new LexemeKey(LexemeType.Word, lexeme.LexicalForm), out wf))
					guid = wf.Guid.ToString();
			}
			else
			{
				toolName = "lexiconEdit";
				var entryLexeme = (FdoLexEntryLexeme)lexeme;
				ILexEntry entry;
				if (TryGetEntry(entryLexeme.Key, out entry))
					guid = entry.Guid.ToString();
			}
			if (guid != null)
			{
				string url = string.Format("silfw://localhost/link?app=flex&database={0}&tool={1}&guid={2}",
					HttpUtility.UrlEncode(m_cache.ProjectId.Name), HttpUtility.UrlEncode(toolName), HttpUtility.UrlEncode(guid));
				// TODO: this would probably be faster if we directly called the RPC socket if FW is already open
				if (Platform.IsUnix)
				{
					string libPath = Path.GetDirectoryName(FileUtils.StripFilePrefix(Assembly.GetExecutingAssembly().CodeBase));
					using (Process.Start(Path.Combine(libPath, "run-app"), string.Format("FieldWorks.exe {0}", url))) {}
				}
				else
				{
					using (Process.Start(url)) {}
				}
			}
		}

		public IEnumerable<string> ValidLanguages
		{
			get
			{
				return m_cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems.Select(writingSystem => writingSystem.Id).ToArray();
			}
		}
	  #endregion

	#region LexiconV2 Implementation

		public bool EncodesKeyInId => false;

		public IEnumerable<Lexeme> FindAllHomographs(LexemeType type, string lexicalForm)
		{
			// If this is looking for anything other than a wordform build the list out of the lex entries
			if (type != LexemeType.Word)
			{
			  var lexEntries = GetMatchingEntries(new LexemeKey(type, lexicalForm));
			  return lexEntries.Select(x => new FdoLexEntryLexeme(this,
				  new LexemeKey(type, x.Guid, Cache)));
			}
			// otherwise throw for now, I don't think we need this
			throw new NotImplementedException($"FindAllHomographs not yet implemented for {type}");
		}
	#endregion

	  #region WordAnalyses implementation
		public WordAnalysis CreateWordAnalysis(string word, IEnumerable<Lexeme> lexemes)
		{
			return new FdoWordAnalysis(word, lexemes);
		}

		public IEnumerable<WordAnalysis> GetWordAnalyses(string word)
		{
			IWfiWordform wordform;
			if (!TryGetWordform(new LexemeKey(LexemeType.Word, word), out wordform))
				return Enumerable.Empty<WordAnalysis>();

			var analyses = new HashSet<WordAnalysis>();
			foreach (IWfiAnalysis analysis in wordform.AnalysesOC.Where(a => a.MorphBundlesOS.Count > 0 && a.ApprovalStatusIcon == (int) Opinions.approves))
			{
				WordAnalysis lexemes;
				if (GetWordAnalysis(analysis, out lexemes))
					analyses.Add(lexemes);
			}
			return analyses;
		}

		public void AddWordAnalysis(WordAnalysis lexemes)
		{
			NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
				{
					IWfiWordform wordform;
					if (!TryGetWordform(new LexemeKey(LexemeType.Word, lexemes.Word), out wordform))
					{
						wordform = m_cache.ServiceLocator.GetInstance<IWfiWordformFactory>().Create(
							TsStringUtils.MakeString(lexemes.Word.Normalize(NormalizationForm.FormD), DefaultVernWs));
					}

					IWfiAnalysis analysis = m_cache.ServiceLocator.GetInstance<IWfiAnalysisFactory>().Create();
					wordform.AnalysesOC.Add(analysis);
					analysis.ApprovalStatusIcon = (int) Opinions.approves;

					foreach (Lexeme lexeme in lexemes)
					{
						var entryLexeme = (FdoLexEntryLexeme) lexeme;
						ILexEntry entry;
						if (TryGetEntry(entryLexeme.Key, out entry))
						{
							IWfiMorphBundle mb = m_cache.ServiceLocator.GetInstance<IWfiMorphBundleFactory>().Create();
							analysis.MorphBundlesOS.Add(mb);
							mb.MorphRA = entry.LexemeFormOA;
							mb.SenseRA = entry.SensesOS[0];
							mb.MsaRA = entry.SensesOS[0].MorphoSyntaxAnalysisRA;
						}
					}
				});
		}

		public void RemoveWordAnalysis(WordAnalysis lexemes)
		{
			IWfiWordform wordform;
			if (!TryGetWordform(new LexemeKey(LexemeType.Word, lexemes.Word), out wordform))
				return;

			foreach (IWfiAnalysis analysis in wordform.AnalysesOC.Where(a => a.MorphBundlesOS.Count > 0 && a.ApprovalStatusIcon == (int) Opinions.approves))
			{
				bool match = true;
				int i = 0;
				foreach (Lexeme lexeme in lexemes)
				{
					if (i == analysis.MorphBundlesOS.Count || analysis.MorphBundlesOS[i].MorphRA == null)
					{
						match = false;
						break;
					}

					var entry = analysis.MorphBundlesOS[i].MorphRA.OwnerOfClass<ILexEntry>();
					if (!GetEntryLexeme(entry).Equals(lexeme))
					{
						match = false;
						break;
					}
					i++;
				}
				if (match && !analysis.OccurrencesInTexts.Any())
				{
					NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () => wordform.AnalysesOC.Remove(analysis));
					break;
				}
			}
		}

		public IEnumerable<WordAnalysis> WordAnalyses
		{
			get
			{
				var analyses = new HashSet<WordAnalysis>();
				foreach (IWfiAnalysis analysis in m_cache.ServiceLocator.GetInstance<IWfiAnalysisRepository>().AllInstances()
					.Where(a => a.MorphBundlesOS.Count > 0 && a.ApprovalStatusIcon == (int) Opinions.approves))
				{
					WordAnalysis lexemes;
					string wordFormWs = analysis.Wordform.Form.get_String(m_defaultVernWs).Text;
					if (wordFormWs != null && GetWordAnalysis(analysis, out lexemes))
						analyses.Add(lexemes);
				}
				return analyses;
			}
		}
	  #endregion

		#region WordAnaysesV2
		public void RemoveWordAnalysis(WordAnalysisV2 lexemes)
		{
			if(Cache.ServiceLocator.GetInstance<IWfiAnalysisRepository>().TryGetObject(new Guid(lexemes.Id), out var wfiAnalysis))
			{
				// If this analysis is used in FLEx in any texts then do not remove it, we claim the ownership and rights to delete
				if (!wfiAnalysis.OccurrencesInTexts.Any())
				{
					using (new NonUndoableUnitOfWorkHelper(Cache.ActionHandlerAccessor))
					{
						wfiAnalysis.Delete();
					}
				}
			}
		}

		IEnumerable<WordAnalysisV2> WordAnalysesV2.WordAnalyses
		{
			get
			{
				var analyses = new HashSet<WordAnalysisV2>();
				foreach (IWfiAnalysis analysis in m_cache.ServiceLocator.GetInstance<IWfiAnalysisRepository>().AllInstances()
							 .Where(a => a.MorphBundlesOS.Count > 0 && a.ApprovalStatusIcon == (int)Opinions.approves))
				{
					var wordFormWs = analysis.Wordform.Form.get_String(m_defaultVernWs).Text;
					if (wordFormWs != null && TryGetWordAnalysis(analysis, out WordAnalysisV2 lexemes))
						analyses.Add(lexemes);
				}
				return analyses;
			}
		}

		WordAnalysisV2 WordAnalysesV2.CreateWordAnalysis(string word, IEnumerable<Lexeme> lexemes)
		{
			return new FdoWordAnalysisV2(word, lexemes);
		}

		IEnumerable<WordAnalysisV2> WordAnalysesV2.GetWordAnalyses(string word)
		{
			if (!TryGetWordform(new LexemeKey(LexemeType.Word, word), out var wordform))
				return Enumerable.Empty<WordAnalysisV2>();

			var analyses = new HashSet<WordAnalysisV2>();
			foreach (var analysis in wordform.AnalysesOC.Where(a =>
					   a.MorphBundlesOS.Count > 0 &&
					   a.ApprovalStatusIcon == (int)Opinions.approves))
			{
				if (TryGetWordAnalysis(analysis, out WordAnalysisV2 lexemes))
				{
					analyses.Add(lexemes);
				}
			}

			return analyses;
		}

		public void AddWordAnalysis(WordAnalysisV2 lexemes)
		{
			NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
			{
				IWfiWordform wordform;
				if (!TryGetWordform(new LexemeKey(LexemeType.Word, new Guid(lexemes.Id), Cache), out wordform))
					wordform = m_cache.ServiceLocator.GetInstance<IWfiWordformFactory>().Create(
						TsStringUtils.MakeString(lexemes.Word.Normalize(NormalizationForm.FormD),
							DefaultVernWs));

				var analysis = m_cache.ServiceLocator.GetInstance<IWfiAnalysisFactory>().Create();
				wordform.AnalysesOC.Add(analysis);
				analysis.ApprovalStatusIcon = (int)Opinions.approves;

				foreach (var lexeme in lexemes)
				{
					var entryLexeme = (FdoLexEntryLexeme)lexeme;
					ILexEntry entry;
					if (TryGetEntry(entryLexeme.Key, out entry))
					{
						var mb = m_cache.ServiceLocator.GetInstance<IWfiMorphBundleFactory>()
							.Create();
						analysis.MorphBundlesOS.Add(mb);
						mb.MorphRA = entry.LexemeFormOA;
						mb.SenseRA = entry.SensesOS[0];
						mb.MsaRA = entry.SensesOS[0].MorphoSyntaxAnalysisRA;
					}
				}
			});
		}
		#endregion

		private bool GetWordAnalysis(IWfiAnalysis analysis, out WordAnalysis lexemes)
		{
			var lexemeArray = new Lexeme[analysis.MorphBundlesOS.Count];
			for (int i = 0; i < analysis.MorphBundlesOS.Count; i++)
			{
				IWfiMorphBundle mb = analysis.MorphBundlesOS[i];
				if (mb.MorphRA == null)
				{
					lexemes = null;
					return false;
				}
				var entry = mb.MorphRA.OwnerOfClass<ILexEntry>();
				if (entry.LexemeFormOA != mb.MorphRA)
				{
					lexemes = null;
					return false;
				}
				lexemeArray[i] = GetEntryLexeme(entry);
			}

			lexemes = new FdoWordAnalysis(analysis.Wordform.Form.get_String(DefaultVernWs).Text.Normalize(), lexemeArray);
			return true;
		}

		private bool TryGetWordAnalysis(IWfiAnalysis analysis, out WordAnalysisV2 lexemes)
		{
			var lexemeArray = new Lexeme[analysis.MorphBundlesOS.Count];
			for (int i = 0; i < analysis.MorphBundlesOS.Count; i++)
			{
				IWfiMorphBundle mb = analysis.MorphBundlesOS[i];
				if (mb.MorphRA == null)
				{
					lexemes = null;
					return false;
				}
				var entry = mb.MorphRA.OwnerOfClass<ILexEntry>();
				if (entry.LexemeFormOA != mb.MorphRA)
				{
					lexemes = null;
					return false;
				}
				lexemeArray[i] = GetEntryLexeme(entry);
			}

			lexemes = new FdoWordAnalysisV2(analysis.Wordform.Guid,
				analysis.Wordform.Form.get_String(DefaultVernWs).Text.Normalize(),
				lexemeArray,
				analysis.MeaningsOC);

			return true;
		}

		private ILexEntry GetMatchingEntryFromParser(string wordForm)
		{
			ILexEntry matchingEntry = null;
			if (m_parser == null)
				InstantiateParser();

			if (m_parser == null)
				return null;

			if (!m_parser.IsUpToDate())
				m_parser.Update();

			ParseResult parseResult = m_parser.ParseWord(wordForm.Replace(' ', '.'));
			if (parseResult != null && parseResult.Analyses != null && parseResult.Analyses.Any())
			{
				foreach (ParseMorph morph in parseResult.Analyses.First().Morphs)
				{
					if (morph.Form is IMoStemAllomorph)
					{
						matchingEntry = morph.Form.OwnerOfClass<ILexEntry>();
						break;
					}
				}
			}
			return matchingEntry;
		}

		private void InstantiateParser()
		{
			string parserDataDir = Path.Combine(ParatextLexiconPluginDirectoryFinder.CodeDirectory, "Language Explorer");
			switch (m_cache.LanguageProject.MorphologicalDataOA.ActiveParser)
			{
				case "XAmple":
					m_parser = new XAmpleParser(m_cache, parserDataDir);
					break;
				case "HC":
					m_parser = new HCParser(m_cache);
					break;
				default:
					throw new InvalidOperationException("The language project is set to use an unrecognized parser.");
			}
		}

		private ILexEntry GetMatchingEntryFromStemmer(string wordForm)
		{
			var repo = m_cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			if (m_stemmer == null)
			{
				m_stemmer = new PoorMansStemmer<string, char>(s => s) {NormalizeScores = true, WeightScores = false, Threshold = 0.12};
				var forms = new HashSet<string>();
				foreach (ILexEntry entry in repo.AllInstances())
				{
					if (!(entry.LexemeFormOA is IMoStemAllomorph))
						continue;

					forms.UnionWith(GetForms(entry));
				}
				m_stemmer.Train(forms);
			}

			int bestLen = 0;
			double bestScore = 0;
			ILexEntry bestMatch = null;
			foreach (ILexEntry entry in repo.AllInstances())
			{
				if (!(entry.LexemeFormOA is IMoStemAllomorph))
					continue;

				foreach (string form in GetForms(entry))
				{
					double formScore;
					if (m_stemmer.HaveSameStem(wordForm, form, out formScore))
					{
						if (formScore > bestScore)
						{
							bestMatch = entry;
							bestScore = formScore;
							bestLen = LongestCommonSubstringLength(wordForm, form);
						}
						else if (Math.Abs(formScore - bestScore) < double.Epsilon)
						{
							int len = LongestCommonSubstringLength(wordForm, form);
							if (len > bestLen)
							{
								bestMatch = entry;
								bestScore = formScore;
								bestLen = len;
							}
						}
					}
				}
			}
			return bestMatch;
		}

		private IEnumerable<string> GetForms(ILexEntry entry)
		{
			ITsString citationFormTss = entry.CitationForm.StringOrNull(m_defaultVernWs);
			if (citationFormTss != null)
				yield return citationFormTss.Text;

			if (entry.LexemeFormOA != null)
			{
				ITsString lexemeFormTss = entry.LexemeFormOA.Form.StringOrNull(m_defaultVernWs);
				if (lexemeFormTss != null)
					yield return lexemeFormTss.Text;
			}
		}

		private static int LongestCommonSubstringLength(string str1, string str2)
		{
			if (String.IsNullOrEmpty(str1) || String.IsNullOrEmpty(str2))
				return 0;

			var num = new int[str1.Length, str2.Length];
			int maxlen = 0;

			for (int i = 0; i < str1.Length; i++)
			{
				for (int j = 0; j < str2.Length; j++)
				{
					if (str1[i] != str2[j])
					{
						num[i, j] = 0;
					}
					else
					{
						if ((i == 0) || (j == 0))
							num[i, j] = 1;
						else
							num[i, j] = 1 + num[i - 1, j - 1];

						if (num[i, j] > maxlen)
							maxlen = num[i, j];
					}
				}
			}
			return maxlen;
		}

		private IEnumerable<ILexEntry> GetMatchingEntries(LexemeKey key)
		{
			// Version 2 key
			if (Guid.TryParse(key.Id, out var guid))
			{
				var entryInList = new List<ILexEntry>();
				if (Cache.ServiceLocator.GetInstance<ILexEntryRepository>()
					.TryGetObject(guid, out var entry))
				{
					entryInList.Add(entry);
				}

				return entryInList;
			}
			CreateEntryIndexIfNeeded();
			SortedSet<ILexEntry> entries;
			if (m_entryIndex.TryGetValue(new LexemeKey(key.Type, key.LexicalForm), out entries))
				return entries;
			return Enumerable.Empty<ILexEntry>();
		}

		private void CreateEntryIndexIfNeeded()
		{
			if (m_entryIndex != null)
				return;

			m_entryIndex = new Dictionary<LexemeKey, SortedSet<ILexEntry>>();
			foreach (ILexEntry entry in m_cache.ServiceLocator.GetInstance<ILexEntryRepository>().AllInstances())
			{
				LexemeType type = GetLexemeTypeForMorphType(entry.PrimaryMorphType);
				string form = entry.LexemeFormOA == null ? string.Empty : entry.LexemeFormOA.Form.VernacularDefaultWritingSystem.Text ?? string.Empty;
				var key = new LexemeKey(type, form.Normalize());

				SortedSet<ILexEntry> entries;
				if (!m_entryIndex.TryGetValue(key, out entries))
				{
					entries = new SortedSet<ILexEntry>(m_entryComparer);
					m_entryIndex[key] = entries;
				}

				HomographNumber hn = m_homographNumbers.GetOrCreateValue(entry);
				if (hn.Number == 0)
				{
					hn.Number = GetParatextHomographNumFromEntry(entry);
				}

				// If we ever have two entries that match by key and have the same homograph # we are in trouble
				Debug.Assert(entries.Select(e => e.HomographNumber).Distinct().Count() == entries.Count);

				entries.Add(entry);
			}
		}

		internal bool TryGetWordform(LexemeKey key, out IWfiWordform wordform)
		{
			if (key.IsGuidKey)
			{
				throw new ArgumentException("Unexpected key type for wordform");
			}
			return m_cache.ServiceLocator.GetInstance<IWfiWordformRepository>().TryGetObject(
				TsStringUtils.MakeString(key.LexicalForm.Normalize(NormalizationForm.FormD), DefaultVernWs), true, out wordform);
		}

		internal bool TryGetAnalysis(LexemeKey key, out IWfiAnalysis analysis)
		{
			if (key.IsGuidKey)
			{
				return Cache.ServiceLocator.GetInstance<IWfiAnalysisRepository>().TryGetObject(new Guid(key.Id), out analysis);
			}

			throw new ArgumentException("Unexpected key type.");
		}

		internal IWfiWordform CreateWordform(string lexicalForm)
		{
			ITsString tss = TsStringUtils.MakeString(lexicalForm.Normalize(NormalizationForm.FormD), DefaultVernWs);
			IWfiWordform wordform = m_cache.ServiceLocator.GetInstance<IWfiWordformFactory>().Create(tss);
			return wordform;
		}

		internal bool TryGetEntry(LexemeKey key, out ILexEntry entry)
		{

			entry = GetMatchingEntries(key).FirstOrDefault(e => m_homographNumbers.GetOrCreateValue(e).Number == key.Homograph);
			return entry != null;
		}

		internal ILexEntry CreateEntry(LexemeKey key)
		{
			CreateEntryIndexIfNeeded();

			ITsString tss = TsStringUtils.MakeString(key.LexicalForm.Normalize(NormalizationForm.FormD), DefaultVernWs);
			var msa = new SandboxGenericMSA {MsaType = (key.Type == LexemeType.Stem) ? MsaType.kStem : MsaType.kUnclassified};
			ILexEntry entry = m_cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(GetMorphTypeForLexemeType(key.Type), tss, (ITsString) null, msa);
			entry.ImportResidue = TsStringUtils.MakeString(AddedByParatext, Cache.DefaultAnalWs);
			m_homographNumbers.GetOrCreateValue(entry).Number = GetParatextHomographNumFromEntry(entry);

			var homographKey = new LexemeKey(key.Type, key.LexicalForm); // as in a key that matches all homographs
			SortedSet<ILexEntry> entries;
			if (!m_entryIndex.TryGetValue(homographKey, out entries))
			{
				entries = new SortedSet<ILexEntry>(m_entryComparer);
				m_entryIndex[homographKey] = entries;
			}
			entries.Add(entry);

			return entry;
		}

		// Paratext 9.3 and earlier treats an entry with no homographs as having a number of 1
		// In LCM the homograph number is 0 if there are no homographs, and counts from 1 otherwise
		internal static int GetParatextHomographNumFromEntry(ILexEntry entry)
		{
			return entry.HomographNumber == 0 ? 1 : entry.HomographNumber;
		}

		internal FdoLexEntryLexeme GetEntryLexeme(ILexEntry entry)
		{
			CreateEntryIndexIfNeeded();
			var type = GetLexemeTypeForMorphType(entry.PrimaryMorphType);
			var hn = m_homographNumbers.GetOrCreateValue(entry);
			// A 0 return value means that this entry was not yet in the table
			if (hn.Number == 0)
			{
				hn.Number = GetParatextHomographNumFromEntry(entry);
			}
			var form = entry.LexemeFormOA == null ? string.Empty : entry.LexemeFormOA.Form.VernacularDefaultWritingSystem.Text ?? string.Empty;
			return new FdoLexEntryLexeme(this, new LexemeKey(type, form.Normalize(), hn.Number));
		}

		internal void OnLexemeAdded(Lexeme lexeme)
		{
			LexemeAdded?.Invoke(this, new FdoLexemeAddedEventArgs(lexeme));
		}

		internal void OnLexiconSenseAdded(Lexeme lexeme, LexiconSense sense)
		{
			LexiconSenseAdded?.Invoke(this, new FdoLexiconSenseAddedEventArgs(lexeme, sense));
		}

		internal void OnLexiconGlossAdded(Lexeme lexeme, LexiconSense sense, LanguageText gloss)
		{
			LexiconGlossAdded?.Invoke(this, new FdoLexiconGlossAddedEventArgs(lexeme, sense, gloss));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the lexeme type that most closely represents the specified morph type.
		/// </summary>
		/// <remarks>This method attempts to do it's best to get the correct lexeme type.
		/// However, the FW database contains many more morph types then can be represented with
		/// the few lexeme types. This creates some ambiguous mappings which are commented
		/// inside this method body.</remarks>
		/// ------------------------------------------------------------------------------------
		private static LexemeType GetLexemeTypeForMorphType(IMoMorphType type)
		{
			if (type != null)
			{
				switch (type.Guid.ToString())
				{
					case MoMorphTypeTags.kMorphCircumfix:
					case MoMorphTypeTags.kMorphInfix:
					case MoMorphTypeTags.kMorphInfixingInterfix:
					case MoMorphTypeTags.kMorphSimulfix:
					case MoMorphTypeTags.kMorphSuprafix:
					case MoMorphTypeTags.kMorphClitic:
					case MoMorphTypeTags.kMorphProclitic:
						// These don't map neatly to a lexeme type, so we just return prefix
						return LexemeType.Prefix;

					case MoMorphTypeTags.kMorphEnclitic:
						// This one also isn't a great match, but there is no better choice
						return LexemeType.Suffix;

					case MoMorphTypeTags.kMorphPrefix:
					case MoMorphTypeTags.kMorphPrefixingInterfix:
						return LexemeType.Prefix;

					case MoMorphTypeTags.kMorphSuffix:
					case MoMorphTypeTags.kMorphSuffixingInterfix:
						return LexemeType.Suffix;

					case MoMorphTypeTags.kMorphPhrase:
					case MoMorphTypeTags.kMorphDiscontiguousPhrase:
						return LexemeType.Phrase;

					case MoMorphTypeTags.kMorphStem:
					case MoMorphTypeTags.kMorphRoot:
					case MoMorphTypeTags.kMorphBoundRoot:
					case MoMorphTypeTags.kMorphBoundStem:
					case MoMorphTypeTags.kMorphParticle:
						return LexemeType.Stem;
				}
			}

			// Shouldn't ever get here, but since we don't know what type it is just return
			// a random default and hope for the best.
			return LexemeType.Stem;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the morph type for the specified lexeme type.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private IMoMorphType GetMorphTypeForLexemeType(LexemeType type)
		{
			if (type == LexemeType.Word || type == LexemeType.Lemma)
				throw new ArgumentException("Morph type can never be of the specified lexeme type");

			var repo = m_cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>();
			switch (type)
			{
				case LexemeType.Prefix: return repo.GetObject(MoMorphTypeTags.kguidMorphPrefix);
				case LexemeType.Suffix: return repo.GetObject(MoMorphTypeTags.kguidMorphSuffix);
				case LexemeType.Phrase: return repo.GetObject(MoMorphTypeTags.kguidMorphPhrase);
				case LexemeType.Stem: return repo.GetObject(MoMorphTypeTags.kguidMorphStem);
			}
			return null;
		}

		void IVwNotifyChange.PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			ICmObject obj = m_cache.ServiceLocator.GetObject(hvo);
			switch (obj.ClassID)
			{
				case LexDbTags.kClassId:
					if (tag == m_cache.ServiceLocator.GetInstance<Virtuals>().LexDbEntries)
					{
						if (!UpdatingEntries)
							m_entryIndex = null;
						m_stemmer = null;
					}
					break;

				case MoStemAllomorphTags.kClassId:
					if (tag == MoFormTags.kflidForm)
					{
						if (!UpdatingEntries && obj.OwningFlid == LexEntryTags.kflidLexemeForm)
							m_entryIndex = null;
						m_stemmer = null;
					}
					break;

				case MoAffixAllomorphTags.kClassId:
					if (!UpdatingEntries && obj.OwningFlid == LexEntryTags.kflidLexemeForm && tag == MoFormTags.kflidForm)
						m_entryIndex = null;
					break;

				case LexEntryTags.kClassId:
					var entry = (ILexEntry) obj;
					switch (tag)
					{
						case LexEntryTags.kflidLexemeForm:
							if (!UpdatingEntries)
								m_entryIndex = null;
							if (entry.LexemeFormOA is IMoStemAllomorph)
								m_stemmer = null;
							break;

						case LexEntryTags.kflidAlternateForms:
							if (entry.LexemeFormOA is IMoStemAllomorph)
								m_stemmer = null;
							break;
					}
					break;

				case MoMorphDataTags.kClassId:
					if (tag == MoMorphDataTags.kflidParserParameters)
					{
						if (m_parser != null)
						{
							m_parser.Dispose();
							m_parser = null;
						}
					}
					break;
			}
		}

		internal class HomographNumber
		{
			public int Number { get; set; }
		}

		private class LexEntryComparer : IComparer<ILexEntry>
		{
			private readonly FdoLexicon m_lexicon;

			public LexEntryComparer(FdoLexicon lexicon)
			{
				m_lexicon = lexicon;
			}

			public int Compare(ILexEntry x, ILexEntry y)
			{
				HomographNumber hnx = m_lexicon.m_homographNumbers.GetOrCreateValue(x);
				HomographNumber hny = m_lexicon.m_homographNumbers.GetOrCreateValue(y);
				return hnx.Number.CompareTo(hny.Number);
			}
		}
	}
}
