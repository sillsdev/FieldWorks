// Copyright (c) 2011-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.ServiceModel;
using LanguageExplorer;
using LanguageExplorer.LcmUi;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using SIL.LCModel.Utils;
using SIL.Reporting;

namespace SIL.FieldWorks.LexicalProvider
{
	/// <summary>
	/// This class implements the ILexicalProvider
	/// </summary>
	[ServiceBehavior(IncludeExceptionDetailInFaults = true, InstanceContextMode = InstanceContextMode.Single, MaxItemsInObjectGraph = 2147483647)]
	public sealed class LexicalProviderImpl : ILexicalProvider
	{
		private const string kAnalysisPrefix = "Analysis:";
		private readonly LcmCache m_cache;

		/// <summary />
		public LexicalProviderImpl(LcmCache cache)
		{
			m_cache = cache;
		}

		#region ILexicalProvider Members

		/// <inheritdoc />
		public void ShowEntry(string entry, EntryType entryType)
		{
			LexicalProviderManager.ResetLexicalProviderTimer();
			Logger.WriteEvent("Showing entry from external application for the " + entryType + " " + entry);

			if (entryType != EntryType.Word)
			{
				throw new ArgumentException("Unknown entry type specified.");
			}
			// An asynchronous call is necessary because the WCF server (FieldWorks) will not
			// respond until this method returns. This also allows methods that show dialogs on the
			// WCF server to not be OneWay. (Otherwise, time-out exceptions occur.)
			FieldWorks.ThreadHelper.InvokeAsync(() =>
			{
				var tss = TsStringUtils.MakeString(entry, FieldWorks.Cache.DefaultVernWs);
				IPublisher publisher = new MyDoNothingPublisher();
				ISubscriber subscriber = new MyDoNothingSubscriber();
				IPropertyTable propertyTable = new MyDoAlmostNothingPropertyTable();
				var styleSheet = FwUtils.StyleSheetFromPropertyTable(propertyTable);
				styleSheet.Init(FieldWorks.Cache, FieldWorks.Cache.LanguageProject.Hvo, LangProjectTags.kflidStyles);
				LexEntryUi.DisplayEntries(FieldWorks.Cache, null, new FlexComponentParameters(propertyTable, publisher, subscriber), new FlexHelpTopicProvider(), "UserHelpFile", tss, null);
			});
		}

		/// <inheritdoc />
		public void ShowRelatedWords(string entry, EntryType entryType)
		{
			LexicalProviderManager.ResetLexicalProviderTimer();
			Logger.WriteEvent("Showing related word from external application for the " + entryType + " " + entry);

			if (entryType != EntryType.Word)
			{
				throw new ArgumentException("Unknown entry type specified.");
			}
			// An asynchronous call is necessary because the WCF server (FieldWorks) will not
			// respond until this method returns. This also allows methods that show dialogs on the
			// WCF server to not be OneWay. (Otherwise, time-out exceptions occur.)
			FieldWorks.ThreadHelper.InvokeAsync(() =>
			{
				var styleSheet = new LcmStyleSheet();
				styleSheet.Init(FieldWorks.Cache, FieldWorks.Cache.LanguageProject.Hvo, LangProjectTags.kflidStyles);
				LexEntryUi.DisplayRelatedEntries(FieldWorks.Cache, null, styleSheet, new FlexHelpTopicProvider(), "UserHelpFile", TsStringUtils.MakeString(entry, FieldWorks.Cache.DefaultVernWs), true);
			});
		}

		/// <inheritdoc />
		public IEnumerable<LexicalEntry> Lexemes()
		{
			LexicalProviderManager.ResetLexicalProviderTimer();

			var entries = new List<LexicalEntry>();
			foreach (var dbEntry in m_cache.ServiceLocator.GetInstance<ILexEntryRepository>().AllInstances())
			{
				var morphType = dbEntry.PrimaryMorphType;
				if (morphType != null)
				{
					entries.Add(CreateEntryFromDbEntry(GetLexemeTypeForMorphType(morphType), dbEntry));
				}
			}
			// Get all of the lexical entries in the database

			// Get all the wordforms in the database
			foreach (var wordform in m_cache.ServiceLocator.GetInstance<IWfiWordformRepository>().AllInstances())
			{
				entries.Add(CreateEntryFromDbWordform(wordform));
			}

			return entries;
		}

		/// <inheritdoc />
		public LexicalEntry GetLexeme(LexemeType type, string lexicalForm, int homograph)
		{
			LexicalProviderManager.ResetLexicalProviderTimer();

			switch (type)
			{
				case LexemeType.Word:
					var wf = GetDbWordform(lexicalForm);
					return wf != null ? CreateEntryFromDbWordform(wf) : null;
				default:
					var dbEntry = GetDbLexeme(type, lexicalForm, homograph);
					return dbEntry != null ? CreateEntryFromDbEntry(type, dbEntry) : null;
			}
		}

		/// <inheritdoc />
		public void AddLexeme(LexicalEntry lexeme)
		{
			LexicalProviderManager.ResetLexicalProviderTimer();
			Logger.WriteEvent("Adding new lexeme from an external application: " + lexeme.LexicalForm);

			NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
			{
				var sForm = lexeme.LexicalForm;
				switch (lexeme.Type)
				{
					case LexemeType.Word:
						var tss = TsStringUtils.MakeString(lexeme.LexicalForm, m_cache.DefaultVernWs);
						m_cache.ServiceLocator.GetInstance<IWfiWordformFactory>().Create(tss);
						break;
					default:
						{
							var msa = new SandboxGenericMSA
							{
								MsaType = lexeme.Type == LexemeType.Stem ? MsaType.kStem : MsaType.kUnclassified
							};

							var morphType = GetMorphTypeForLexemeType(lexeme.Type);
							var tssForm = TsStringUtils.MakeString(sForm, m_cache.DefaultVernWs);
							m_cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(morphType, tssForm, (ITsString)null, msa);
							break;
						}
				}
			});
		}

		/// <inheritdoc />
		public LexSense AddSenseToEntry(LexemeType type, string lexicalForm, int homograph)
		{
			LexicalProviderManager.ResetLexicalProviderTimer();
			Logger.WriteEvent("Adding new sense to lexeme '" + lexicalForm + "' from an external application");

			return NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
			{
				var guid = string.Empty;
				switch (type)
				{
					case LexemeType.Word:
						{
							var dbWordform = GetDbWordform(lexicalForm);
							if (dbWordform == null)
							{
								throw new ArgumentException("Entry in the lexicon not found for the specified information");
							}
							// For wordforms, our "senses" could be new meanings of an analysis for the word
							// or it could be a brand new analysis. Because we have no idea what the user actually
							// wanted, we just assume the worst (they want to create a new analysis for the word
							// with a new meaning).
							var dbAnalysis = m_cache.ServiceLocator.GetInstance<IWfiAnalysisFactory>().Create();
							dbWordform.AnalysesOC.Add(dbAnalysis);
							guid = kAnalysisPrefix + dbAnalysis.Guid;
							break;
						}
					default:
						{
							var dbEntry = GetDbLexeme(type, lexicalForm, homograph);
							if (dbEntry == null)
							{
								throw new ArgumentException("Entry in the lexicon not found for the specified information");
							}
							if (dbEntry.SensesOS.Count == 1 && dbEntry.SensesOS[0].Gloss.StringCount == 0)
							{
								// An empty sense exists (probably was created during a call to AddLexeme)
								guid = dbEntry.SensesOS[0].Guid.ToString();
								break;
							}

							var newSense = m_cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create(dbEntry, new SandboxGenericMSA(), (ITsString)null);
							guid = newSense.Guid.ToString();
							break;
						}
				}
				return new LexSense(guid);
			});
		}

		/// <inheritdoc />
		public LexGloss AddGlossToSense(LexemeType type, string lexicalForm, int homograph, string senseId, string language, string text)
		{
			LexicalProviderManager.ResetLexicalProviderTimer();
			Logger.WriteEvent("Adding new gloss to lexeme '" + lexicalForm + "' from an external application");

			return NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
			{
				IMultiUnicode dbGlosses;
				if (senseId.StartsWith(kAnalysisPrefix))
				{
					// The "sense" is actually an analysis for a wordform and our new
					// gloss is a new meaning for that analysis.
					Guid analysisGuid = new Guid(senseId.Substring(kAnalysisPrefix.Length));
					IWfiAnalysis dbAnalysis = m_cache.ServiceLocator.GetInstance<IWfiAnalysisRepository>().GetObject(analysisGuid);
					IWfiGloss dbGloss = dbAnalysis.MeaningsOC.FirstOrDefault();
					if (dbGloss == null)
					{
						dbGloss = m_cache.ServiceLocator.GetInstance<IWfiGlossFactory>().Create();
						dbAnalysis.MeaningsOC.Add(dbGloss);
					}
					dbGlosses = dbGloss.Form;
					dbAnalysis.ApprovalStatusIcon = (int)Opinions.approves; // Assume the analysis from the external application is user approved
				}
				else
				{
					Guid senseGuid = new Guid(senseId);
					ILexSense dbSense = m_cache.ServiceLocator.GetInstance<ILexSenseRepository>().GetObject(senseGuid);
					dbGlosses = dbSense.Gloss;
				}

				// Add the new gloss to the list of glosses for the sense
				var writingSystem = m_cache.WritingSystemFactory.get_Engine(language);
				dbGlosses.set_String(writingSystem.Handle, TsStringUtils.MakeString(text, writingSystem.Handle));

				return new LexGloss(language, text);
			});
		}

		/// <inheritdoc />
		public void RemoveGloss(LexemeType type, string lexicalForm, int homograph, string senseId, string language)
		{
			LexicalProviderManager.ResetLexicalProviderTimer();
			Logger.WriteEvent("Removing gloss from lexeme '" + lexicalForm + "' from an external application");

			NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
			{
				IMultiUnicode dbGlosses;
				Guid guid;
				if (senseId.StartsWith(kAnalysisPrefix))
				{
					// The "sense" is actually an analysis for a wordform and the gloss
					// we want to delete is a meaning for that analysis.
					guid = new Guid(senseId.Substring(kAnalysisPrefix.Length));
					var dbAnalysis = m_cache.ServiceLocator.GetInstance<IWfiAnalysisRepository>().GetObject(guid);
					var dbGloss = dbAnalysis.MeaningsOC.First();
					dbGlosses = dbGloss.Form;
				}
				else
				{
					guid = new Guid(senseId);
					var dbSense = m_cache.ServiceLocator.GetInstance<ILexSenseRepository>().GetObject(guid);
					dbGlosses = dbSense.Gloss;
				}
				// Remove the gloss from the list of glosses for the sense
				var wsId = m_cache.WritingSystemFactory.GetWsFromStr(language);
				dbGlosses.set_String(wsId, (ITsString)null);

				// Delete the sense if there are no more glosses for it
				if (dbGlosses.StringCount == 0)
				{
					RemoveSense(senseId, guid);
				}
			});
		}

		/// <inheritdoc />
		public void Save()
		{
			LexicalProviderManager.ResetLexicalProviderTimer();

			// ENHANCE: We could do a save on the cache here. It doesn't seem really important
			// since the cache will be saved automatically with 10 seconds of inactivity anyways.
		}

		/// <inheritdoc />
		public void BeginChange()
		{
			LexicalProviderManager.ResetLexicalProviderTimer();

			// Ideally, we would like to be able to begin an undo task here and then end it in
			// EndChange(). However, because there is no guarantee that EndChange() will ever
			// get called (and to keep the data in a consistent state), we need to wrap
			// each individual change in it's own undo task.
		}

		/// <inheritdoc />
		public void EndChange()
		{
			LexicalProviderManager.ResetLexicalProviderTimer();
		}
		#endregion

		#region Private helper methods

		/// <summary>
		/// Gets the DB LexEntry for the specified type and form (homograph currently ignored) or
		/// null if none could be found.
		/// </summary>
		private ILexEntry GetDbLexeme(LexemeType type, string lexicalForm, int homograph)
		{
			if (type == LexemeType.Word)
			{
				throw new ArgumentException("LexEntry can not be found for the Lexeme type specified");
			}

			// ENHANCE: We may have to do something with the homograph number eventually, but
			// currently there is no correlation between the homograph number in the DB and
			// the homograph number passed from the external application.
			return m_cache.ServiceLocator.GetInstance<ILexEntryRepository>().GetHomographs(lexicalForm).FirstOrDefault(dbEntry => LexemeTypeAndMorphTypeMatch(type, dbEntry.PrimaryMorphType));
		}

		/// <summary>
		/// Gets the DB Wordform for the form or null if none could be found.
		/// </summary>
		private IWfiWordform GetDbWordform(string lexicalForm)
		{
			IWfiWordform wf;
			m_cache.ServiceLocator.GetInstance<IWfiWordformRepository>().TryGetObject(TsStringUtils.MakeString(lexicalForm, m_cache.DefaultVernWs), true, out wf);
			return wf;
		}

		/// <summary>
		/// Creates a new LexicalEntry from the specified lexical entry in the DB
		/// </summary>
		private LexicalEntry CreateEntryFromDbEntry(LexemeType type, ILexEntry dbEntry)
		{
			if (type == LexemeType.Word)
			{
				throw new ArgumentException("Lexeme type specified can not be created from a LexEntry");
			}

			// A homograph number of zero in the DB means there is only one entry for the wordform.
			// However, the interface requires there be an entry with a homograph of one even if
			// there is only one entry.
			var entry = new LexicalEntry(type, dbEntry.HomographForm, dbEntry.HomographNumber > 0 ? dbEntry.HomographNumber : 1);

			// Add the senses to the interface (non-DB) entry
			foreach (var dbSense in dbEntry.SensesOS)
			{
				var sense = new LexSense(dbSense.Guid.ToString());
				AddDbGlossesToSense(sense, dbSense.Gloss);
				entry.Senses.Add(sense); // Add the sense to the list of senses
			}
			return entry;
		}

		/// <summary>
		/// Creates a new LexicalEntry from the specified word form in the DB
		/// </summary>
		private LexicalEntry CreateEntryFromDbWordform(IWfiWordform wordform)
		{
			const int homograph = 1;
			var entry = new LexicalEntry(LexemeType.Word, wordform.Form.VernacularDefaultWritingSystem.Text, homograph);
			foreach (var dbAnalysis in wordform.AnalysesOC)
			{
				// Since our "sense" is really an analysis for a wordform, assume that any meanings
				// for that analysis are glosses for the same "sense".
				var sense = new LexSense(kAnalysisPrefix + dbAnalysis.Guid.ToString());
				foreach (var gloss in dbAnalysis.MeaningsOC)
				{
					AddDbGlossesToSense(sense, gloss.Form);
				}
				entry.Senses.Add(sense);
			}
			return entry;
		}

		/// <summary>
		/// Adds the glosses in all available writing systems to the specified sense.
		/// </summary>
		private void AddDbGlossesToSense(LexSense sense, IMultiUnicode glosses)
		{
			for (var i = 0; i < glosses.StringCount; i++)
			{
				int ws;
				var tssGloss = glosses.GetStringFromIndex(i, out ws);
				var icuLocale = m_cache.WritingSystemFactory.GetStrFromWs(ws);
				sense.Glosses.Add(new LexGloss(icuLocale, tssGloss.Text));
			}
		}

		/// <summary>
		/// Removes the sense with the specified ID and guid from the DB.
		/// </summary>
		private void RemoveSense(string senseId, Guid guid)
		{
			if (senseId.StartsWith(kAnalysisPrefix))
			{
				var dbAnalysis = m_cache.ServiceLocator.GetInstance<IWfiAnalysisRepository>().GetObject(guid);
				var dbWordform = (IWfiWordform)dbAnalysis.Owner;
				dbWordform.AnalysesOC.Remove(dbAnalysis);
			}
			else
			{
				var dbSense = m_cache.ServiceLocator.GetInstance<ILexSenseRepository>().GetObject(guid);
				var dbEntry = (ILexEntry)dbSense.Owner;

				// Make sure we keep at least one sense around. This seems to be required.
				if (dbEntry.SensesOS.Count > 1)
				{
					dbEntry.SensesOS.Remove(dbSense);
				}
			}
		}
		#endregion

		#region LexemeType and MorphType matching methods

		/// <summary>
		/// Determines if the specified lexeme type matches the specified morph type
		/// </summary>
		private static bool LexemeTypeAndMorphTypeMatch(LexemeType type, IMoMorphType morphType)
		{
			if (type == LexemeType.Word || type == LexemeType.Lemma)
			{
				throw new ArgumentException("Morph type can never be of the specified lexeme type");
			}

			switch (type)
			{
				case LexemeType.Prefix: return morphType.IsPrefixishType;
				case LexemeType.Suffix: return morphType.IsSuffixishType;
				case LexemeType.Stem:
					return morphType.IsStemType &&
  morphType.Guid != MoMorphTypeTags.kguidMorphPhrase &&
  morphType.Guid != MoMorphTypeTags.kguidMorphDiscontiguousPhrase;
				case LexemeType.Phrase:
					return morphType.Guid == MoMorphTypeTags.kguidMorphPhrase || morphType.Guid == MoMorphTypeTags.kguidMorphDiscontiguousPhrase;
			}
			return false;
		}

		/// <summary>
		/// Gets the morph type for the specified lexeme type.
		/// </summary>
		private IMoMorphType GetMorphTypeForLexemeType(LexemeType type)
		{
			if (type == LexemeType.Word || type == LexemeType.Lemma)
			{
				throw new ArgumentException("Morph type can never be of the specified lexeme type");
			}
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

		/// <summary>
		/// Gets the lexeme type that most closely represents the specified morph type.
		/// </summary>
		/// <remarks>This method attempts to do it's best to get the correct lexeme type.
		/// However, the FW database contains many more morph types then can be represented with
		/// the few lexeme types. This creates some ambiguous mappings which are commented
		/// inside this method body.</remarks>
		private LexemeType GetLexemeTypeForMorphType(IMoMorphType type)
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

			// Shouldn't ever get here, but since we don't know what type it is just return
			// a random default and hope for the best.
			return LexemeType.Stem;
		}
		#endregion

		#region Private classes

		/// <summary>
		/// Do nothing IPublisher, since there are no subscribers in the LexicalProviderImpl.
		/// </summary>
		private sealed class MyDoNothingPublisher : IPublisher
		{
			#region Implementation of IPublisher

			/// <inheritdoc />
			public void Publish(string message, object newValue)
			{
				// Pretend to do something.
			}

			/// <inheritdoc />
			public void Publish(IList<string> messages, IList<object> newValues)
			{
				// Pretend to do something.
			}

			#endregion
		}

		/// <summary>
		/// Do nothing ISubscriber, since there are no subscribers in the LexicalProviderImpl.
		/// </summary>
		private class MyDoNothingSubscriber : ISubscriber
		{
			private readonly Dictionary<string, HashSet<Action<object>>> _subscriptions = new Dictionary<string, HashSet<Action<object>>>();

			#region Implementation of ISubscriber

			/// <inheritdoc />
			void ISubscriber.Subscribe(string message, Action<object> messageHandler)
			{
				// Pretend to do something.
			}

			/// <inheritdoc />
			void ISubscriber.Unsubscribe(string message, Action<object> messageHandler)
			{
				// Pretend to do something.
			}

			/// <inheritdoc />
			public IReadOnlyDictionary<string, HashSet<Action<object>>> Subscriptions => new ReadOnlyDictionary<string, HashSet<Action<object>>>(_subscriptions);

			#endregion
		}

		/// <summary>
		/// Do almost nothing IPropertyTable, since there is only one property in the LexicalProviderImpl.
		/// </summary>
		private sealed class MyDoAlmostNothingPropertyTable : IPropertyTable
		{
			private IPropertyRetriever AsIPropertyRetriever => this;

			#region Implementation of IPropertyRetriever

			/// <inheritdoc />
			bool IPropertyRetriever.PropertyExists(string propertyName, SettingsGroup settingsGroup)
			{
				return AsIPropertyRetriever.PropertyExists(propertyName);
			}

			/// <inheritdoc />
			bool IPropertyRetriever.TryGetValue<T>(string name, out T propertyValue, SettingsGroup settingsGroup)
			{
				return AsIPropertyRetriever.TryGetValue<T>(name, out propertyValue);
			}

			/// <inheritdoc />
			T IPropertyRetriever.GetValue<T>(string propertyName, SettingsGroup settingsGroup)
			{
				return AsIPropertyRetriever.GetValue<T>(propertyName);
			}

			/// <inheritdoc />
			T IPropertyRetriever.GetValue<T>(string propertyName, T defaultValue, SettingsGroup settingsGroup)
			{
				return AsIPropertyRetriever.GetValue<T>(propertyName);
			}

			#endregion

			#region Implementation of IDisposable

			/// <inheritdoc />
			public void Dispose()
			{
				throw new NotSupportedException();
			}

			#endregion

			#region Implementation of IPropertyTable

			/// <inheritdoc />
			void IPropertyTable.SetProperty(string name, object newValue, bool persistProperty, bool doBroadcastIfChanged, SettingsGroup settingsGroup)
			{
				throw new NotSupportedException();
			}

			/// <inheritdoc />
			void IPropertyTable.SetDefault(string name, object defaultValue, bool persistProperty, bool doBroadcastIfChanged, SettingsGroup settingsGroup)
			{
				throw new NotSupportedException();
			}

			/// <inheritdoc />
			void IPropertyTable.RemoveProperty(string name, SettingsGroup settingsGroup)
			{
				throw new NotSupportedException();
			}

			/// <inheritdoc />
			void IPropertyTable.ConvertOldPropertiesToNewIfPresent()
			{
				throw new NotSupportedException();
			}

			/// <inheritdoc />
			void IPropertyTable.SetPropertyDispose(string name, bool doDispose, SettingsGroup settingsGroup)
			{
				throw new NotSupportedException();
			}

			/// <inheritdoc />
			string IPropertyTable.UserSettingDirectory { get; set; }

			/// <inheritdoc />
			string IPropertyTable.GlobalSettingsId { get; }

			/// <inheritdoc />
			string IPropertyTable.LocalSettingsId { get; set; }

			/// <inheritdoc />
			void IPropertyTable.RestoreFromFile(string settingsId)
			{
				throw new NotSupportedException();
			}

			/// <inheritdoc />
			void IPropertyTable.SaveGlobalSettings()
			{
				throw new NotSupportedException();
			}

			/// <inheritdoc />
			void IPropertyTable.SaveLocalSettings()
			{
				throw new NotSupportedException();
			}

			#endregion
		}
		#endregion
	}
}
