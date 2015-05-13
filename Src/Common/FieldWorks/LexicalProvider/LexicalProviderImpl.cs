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
// File: LexicalProviderImpl.cs
// Responsibility: FW Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.ServiceModel;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FdoUi;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.LexicalProvider
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This class implements the ILexicalProvider
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[ServiceBehavior(IncludeExceptionDetailInFaults = true,
		InstanceContextMode = InstanceContextMode.Single,
		MaxItemsInObjectGraph = 2147483647)]
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification="m_cache is a reference")]
	public sealed class LexicalProviderImpl : ILexicalProvider
	{
		private const string kAnalysisPrefix = "Analysis:";
		private readonly FdoCache m_cache;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="LexicalProviderImpl"/> class for the
		/// specified cache.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public LexicalProviderImpl(FdoCache cache)
		{
			m_cache = cache;
		}

		#region ILexicalProvider Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Displays the specified entry using the application with the lexical data.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ShowEntry(string entry, EntryType entryType)
		{
			LexicalProviderManager.ResetLexicalProviderTimer();
			Logger.WriteEvent("Showing entry from external application for the " + entryType + " " + entry);

			if (entryType != EntryType.Word)
				throw new ArgumentException("Unknown entry type specified.");

			// An asynchronous call is necessary because the WCF server (FieldWorks) will not
			// respond until this method returns. This also allows methods that show dialogs on the
			// WCF server to not be OneWay. (Otherwise, time-out exceptions occur.)
			FieldWorks.ThreadHelper.InvokeAsync(() =>
			{
				ITsString tss = TsStringUtils.MakeTss(entry, FieldWorks.Cache.DefaultVernWs);
				using (Mediator mediator = new Mediator())
				using (PropertyTable propertyTable = new PropertyTable(mediator))
				{
					propertyTable.SetProperty("HelpTopicProvider", FieldWorks.GetHelpTopicProvider(), true);
					propertyTable.SetPropertyPersistence("HelpTopicProvider", false);
					var flexApp = FieldWorks.GetOrCreateFlexApp();
					propertyTable.SetProperty("FeedbackInfoProvider", flexApp, true);
					propertyTable.SetPropertyPersistence("FeedbackInfoProvider", false);
					propertyTable.SetProperty("App", flexApp, true);

					LexEntryUi.DisplayEntry(FieldWorks.Cache, mediator, propertyTable, propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider"),
						"UserHelpFile", tss, null);
				}
			});
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Displays the related words to the specified entry using the application with the
		/// lexical data.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ShowRelatedWords(string entry, EntryType entryType)
		{
			LexicalProviderManager.ResetLexicalProviderTimer();
			Logger.WriteEvent("Showing related word from external application for the " + entryType + " " + entry);

			if (entryType != EntryType.Word)
				throw new ArgumentException("Unknown entry type specified.");

			// An asynchronous call is necessary because the WCF server (FieldWorks) will not
			// respond until this method returns. This also allows methods that show dialogs on the
			// WCF server to not be OneWay. (Otherwise, time-out exceptions occur.)
			FieldWorks.ThreadHelper.InvokeAsync(() =>
			{
				ITsString tss = TsStringUtils.MakeTss(entry, FieldWorks.Cache.DefaultVernWs);
				using (Mediator mediator = new Mediator())
				using (PropertyTable propertyTable = new PropertyTable(mediator))
				{
					propertyTable.SetProperty("HelpTopicProvider", FieldWorks.GetHelpTopicProvider(), true);
					propertyTable.SetPropertyPersistence("HelpTopicProvider", false);
					var flexApp = FieldWorks.GetOrCreateFlexApp();
					propertyTable.SetProperty("FeedbackInfoProvider", flexApp, true);
					propertyTable.SetPropertyPersistence("FeedbackInfoProvider", false);
					propertyTable.SetProperty("App", flexApp, true);

					LexEntryUi.DisplayRelatedEntries(FieldWorks.Cache, mediator, propertyTable, propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider"),
						"UserHelpFile", tss);
				}
			});
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets all lexemes in the Lexicon
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IEnumerable<LexicalEntry> Lexemes()
		{
			LexicalProviderManager.ResetLexicalProviderTimer();

			List<LexicalEntry> entries = new List<LexicalEntry>();
			// Get all of the lexical entries in the database
			foreach (ILexEntry dbEntry in m_cache.ServiceLocator.GetInstance<ILexEntryRepository>().AllInstances())
			{
				IMoMorphType morphType = dbEntry.PrimaryMorphType;
				if (morphType != null)
					entries.Add(CreateEntryFromDbEntry(GetLexemeTypeForMorphType(morphType), dbEntry));
			}

			// Get all the wordforms in the database
			foreach (IWfiWordform wordform in m_cache.ServiceLocator.GetInstance<IWfiWordformRepository>().AllInstances())
				entries.Add(CreateEntryFromDbWordform(wordform));

			return entries;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Looks up an lexeme
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public LexicalEntry GetLexeme(LexemeType type, string lexicalForm, int homograph)
		{
			LexicalProviderManager.ResetLexicalProviderTimer();

			switch (type)
			{
				case LexemeType.Word:
					IWfiWordform wf = GetDbWordform(lexicalForm);
					return (wf != null) ? CreateEntryFromDbWordform(wf) : null;
				default:
					ILexEntry dbEntry = GetDbLexeme(type, lexicalForm, homograph);
					return (dbEntry != null) ? CreateEntryFromDbEntry(type, dbEntry) : null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the lexeme to the lexicon.
		/// </summary>
		/// <exception cref="ArgumentException">if matching lexeme is already in lexicon</exception>
		/// ------------------------------------------------------------------------------------
		public void AddLexeme(LexicalEntry lexeme)
		{
			LexicalProviderManager.ResetLexicalProviderTimer();
			Logger.WriteEvent("Adding new lexeme from an external application: " + lexeme.LexicalForm);

			NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
				{
					string sForm = lexeme.LexicalForm;
					switch(lexeme.Type)
					{
						case LexemeType.Word:
							ITsString tss = TsStringUtils.MakeTss(lexeme.LexicalForm, m_cache.DefaultVernWs);
							m_cache.ServiceLocator.GetInstance<IWfiWordformFactory>().Create(tss);
							break;
						default:
						{
							SandboxGenericMSA msa = new SandboxGenericMSA();
							msa.MsaType = (lexeme.Type == LexemeType.Stem) ? MsaType.kStem : MsaType.kUnclassified;

							IMoMorphType morphType = GetMorphTypeForLexemeType(lexeme.Type);
							ITsString tssForm = TsStringUtils.MakeTss(sForm, m_cache.DefaultVernWs);
							m_cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create(morphType, tssForm, (ITsString) null, msa);
							break;
						}
					}
				});
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a new sense to the lexeme with the specified information
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public LexSense AddSenseToEntry(LexemeType type, string lexicalForm, int homograph)
		{
			LexicalProviderManager.ResetLexicalProviderTimer();
			Logger.WriteEvent("Adding new sense to lexeme '" + lexicalForm + "' from an external application");

			return NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
				{
					string guid = string.Empty;
					switch (type)
					{
						case LexemeType.Word:
						{
							IWfiWordform dbWordform = GetDbWordform(lexicalForm);
							if (dbWordform == null)
								throw new ArgumentException("Entry in the lexicon not found for the specified information");

							// For wordforms, our "senses" could be new meanings of an analysis for the word
							// or it could be a brand new analysis. Because we have no idea what the user actually
							// wanted, we just assume the worst (they want to create a new analysis for the word
							// with a new meaning).
							IWfiAnalysis dbAnalysis = m_cache.ServiceLocator.GetInstance<IWfiAnalysisFactory>().Create();
							dbWordform.AnalysesOC.Add(dbAnalysis);
							guid = kAnalysisPrefix + dbAnalysis.Guid;
							break;
						}
						default:
						{
							ILexEntry dbEntry = GetDbLexeme(type, lexicalForm, homograph);
							if (dbEntry == null)
								throw new ArgumentException("Entry in the lexicon not found for the specified information");

							if (dbEntry.SensesOS.Count == 1 && dbEntry.SensesOS[0].Gloss.StringCount == 0)
							{
								// An empty sense exists (probably was created during a call to AddLexeme)
								guid = dbEntry.SensesOS[0].Guid.ToString();
								break;
							}

							ILexSense newSense = m_cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create(
								dbEntry, new SandboxGenericMSA(), (ITsString) null);
							guid = newSense.Guid.ToString();
							break;
						}
					}
					return new LexSense(guid);
				});
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a new gloss to the sense with the specified information
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public LexGloss AddGlossToSense(LexemeType type, string lexicalForm, int homograph,
			string senseId, string language, string text)
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
						dbAnalysis.ApprovalStatusIcon = 1; // Assume the analysis from the external application is user approved
					}
					else
					{
						Guid senseGuid = new Guid(senseId);
						ILexSense dbSense = m_cache.ServiceLocator.GetInstance<ILexSenseRepository>().GetObject(senseGuid);
						dbGlosses = dbSense.Gloss;
					}

					// Add the new gloss to the list of glosses for the sense
					ILgWritingSystem writingSystem = m_cache.WritingSystemFactory.get_Engine(language);
					dbGlosses.set_String(writingSystem.Handle, TsStringUtils.MakeTss(text, writingSystem.Handle));

					return new LexGloss(language, text);
				});
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes the gloss with the specified language form the sense with the specified information
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void RemoveGloss(LexemeType type, string lexicalForm, int homograph,
			string senseId, string language)
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
						IWfiAnalysis dbAnalysis = m_cache.ServiceLocator.GetInstance<IWfiAnalysisRepository>().GetObject(guid);
						IWfiGloss dbGloss = dbAnalysis.MeaningsOC.First();
						dbGlosses = dbGloss.Form;
					}
					else
					{
						guid = new Guid(senseId);
						ILexSense dbSense = m_cache.ServiceLocator.GetInstance<ILexSenseRepository>().GetObject(guid);
						dbGlosses = dbSense.Gloss;
					}
					// Remove the gloss from the list of glosses for the sense
					int wsId = m_cache.WritingSystemFactory.GetWsFromStr(language);
					dbGlosses.set_String(wsId, (ITsString)null);

					// Delete the sense if there are no more glosses for it
					if (dbGlosses.StringCount == 0)
						RemoveSense(senseId, guid);
				});
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Forces a save of lexicon
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Save()
		{
			LexicalProviderManager.ResetLexicalProviderTimer();

			// ENHANCE: We could do a save on the cache here. It doesn't seem really important
			// since the cache will be saved automatically with 10 seconds of inactivity anyways.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This must be called before entries are changed to ensure that
		/// it is saved to disk. Since the lexicon is a complex structure
		/// and other features depend on knowing when it is changed,
		/// all work done with the lexicon is marked with a begin and
		/// end change.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void BeginChange()
		{
			LexicalProviderManager.ResetLexicalProviderTimer();

			// Ideally, we would like to be able to begin an undo task here and then end it in
			// EndChange(). However, because there is no guarantee that EndChange() will ever
			// get called (and to keep the data in a consistant state), we need to wrap
			// each individual change in it's own undo task.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This must be called after entries are changed to ensure that
		/// other features dependent on the lexicon are made aware of the
		/// change.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void EndChange()
		{
			LexicalProviderManager.ResetLexicalProviderTimer();
		}
		#endregion

		#region Private helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the DB LexEntry for the specified type and form (homograph currently ignored) or
		/// null if none could be found.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private ILexEntry GetDbLexeme(LexemeType type, string lexicalForm, int homograph)
		{
			if (type == LexemeType.Word)
				throw new ArgumentException("LexEntry can not be found for the Lexeme type specified");

			// ENHANCE: We may have to do something with the homograph number eventually, but
			// currently there is no correlation between the homograph number in the DB and
			// the homograph number passed from the external application.
			return m_cache.ServiceLocator.GetInstance<ILexEntryRepository>().GetHomographs(lexicalForm).FirstOrDefault(
				dbEntry => LexemeTypeAndMorphTypeMatch(type, dbEntry.PrimaryMorphType));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the DB Wordform for the form or null if none could be found.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private IWfiWordform GetDbWordform(string lexicalForm)
		{
			IWfiWordform wf;
			m_cache.ServiceLocator.GetInstance<IWfiWordformRepository>().TryGetObject(
				TsStringUtils.MakeTss(lexicalForm, m_cache.DefaultVernWs), true, out wf);
			return wf;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new LexicalEntry from the specified lexical entry in the DB
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private LexicalEntry CreateEntryFromDbEntry(LexemeType type, ILexEntry dbEntry)
		{
			if (type == LexemeType.Word)
				throw new ArgumentException("Lexeme type specified can not be created from a LexEntry");

			// A homograph number of zero in the DB means there is only one entry for the wordform.
			// However, the interface requires there be an entry with a homograph of one even if
			// there is only one entry.
			LexicalEntry entry = new LexicalEntry(type, dbEntry.HomographForm,
				dbEntry.HomographNumber > 0 ? dbEntry.HomographNumber : 1);

			// Add the senses to the interface (non-DB) entry
			foreach (ILexSense dbSense in dbEntry.SensesOS)
			{
				LexSense sense = new LexSense(dbSense.Guid.ToString());
				AddDbGlossesToSense(sense, dbSense.Gloss);
				entry.Senses.Add(sense); // Add the sense to the list of senses
			}
			return entry;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new LexicalEntry from the specified word form in the DB
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private LexicalEntry CreateEntryFromDbWordform(IWfiWordform wordform)
		{
			const int homograph = 1;
			LexicalEntry entry = new LexicalEntry(LexemeType.Word, wordform.Form.VernacularDefaultWritingSystem.Text, homograph);
			foreach (IWfiAnalysis dbAnalysis in wordform.AnalysesOC)
			{
				// Since our "sense" is really an analysis for a wordform, assume that any meanings
				// for that analysis are glosses for the same "sense".
				LexSense sense = new LexSense(kAnalysisPrefix + dbAnalysis.Guid.ToString());
				foreach (IWfiGloss gloss in dbAnalysis.MeaningsOC)
					AddDbGlossesToSense(sense, gloss.Form);
				entry.Senses.Add(sense);
			}
			return entry;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the glosses in all available writing systems to the specified sense.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void AddDbGlossesToSense(LexSense sense, IMultiUnicode glosses)
		{
			for (int i = 0; i < glosses.StringCount; i++)
			{
				int ws;
				ITsString tssGloss = glosses.GetStringFromIndex(i, out ws);
				string icuLocale = m_cache.WritingSystemFactory.GetStrFromWs(ws);
				sense.Glosses.Add(new LexGloss(icuLocale, tssGloss.Text));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes the sense with the specified ID and guid from the DB.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void RemoveSense(string senseId, Guid guid)
		{
			if (senseId.StartsWith(kAnalysisPrefix))
			{
				IWfiAnalysis dbAnalysis = m_cache.ServiceLocator.GetInstance<IWfiAnalysisRepository>().GetObject(guid);
				IWfiWordform dbWordform = (IWfiWordform)dbAnalysis.Owner;
				dbWordform.AnalysesOC.Remove(dbAnalysis);
			}
			else
			{
				ILexSense dbSense = m_cache.ServiceLocator.GetInstance<ILexSenseRepository>().GetObject(guid);
				ILexEntry dbEntry = (ILexEntry)dbSense.Owner;

				// Make sure we keep at least one sense around. This seems to be required.
				if (dbEntry.SensesOS.Count > 1)
					dbEntry.SensesOS.Remove(dbSense);
			}
		}
		#endregion

		#region LexemeType and MorphType matching methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines if the specified lexeme type matches the specified morph type
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static bool LexemeTypeAndMorphTypeMatch(LexemeType type, IMoMorphType morphType)
		{
			if (type == LexemeType.Word || type == LexemeType.Lemma)
				throw new ArgumentException("Morph type can never be of the specified lexeme type");

			switch (type)
			{
				case LexemeType.Prefix: return morphType.IsPrefixishType;
				case LexemeType.Suffix: return morphType.IsSuffixishType;
				case LexemeType.Stem: return morphType.IsStemType &&
					morphType.Guid != MoMorphTypeTags.kguidMorphPhrase &&
					morphType.Guid != MoMorphTypeTags.kguidMorphDiscontiguousPhrase;
				case LexemeType.Phrase:
					return morphType.Guid == MoMorphTypeTags.kguidMorphPhrase ||
						morphType.Guid == MoMorphTypeTags.kguidMorphDiscontiguousPhrase;
			}
			return false;
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

			IMoMorphTypeRepository repo = m_cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>();
			switch (type)
			{
				case LexemeType.Prefix: return repo.GetObject(MoMorphTypeTags.kguidMorphPrefix);
				case LexemeType.Suffix: return repo.GetObject(MoMorphTypeTags.kguidMorphSuffix);
				case LexemeType.Phrase: return repo.GetObject(MoMorphTypeTags.kguidMorphPhrase);
				case LexemeType.Stem: return repo.GetObject(MoMorphTypeTags.kguidMorphStem);
			}
			return null;
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
	}
}
