// Copyright (c) 2011-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Windows.Forms;
using LanguageExplorer;
using LanguageExplorer.LcmUi;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using SIL.LCModel.Utils;
using SIL.PlatformUtilities;
using SIL.Reporting;
using Timer = System.Threading.Timer;

namespace SIL.FieldWorks.LexicalProvider
{
	/// <summary>
	/// Manages FieldWorks's lexical service provider for access by external applications.
	/// </summary>
	internal static class LexicalProviderManager
	{
		private const int kInactivityTimeout = 1800000; // 30 minutes in msec

		private static Timer s_lexicalProviderTimer;
		private static readonly Dictionary<Type, ServiceHost> s_runningProviders = new Dictionary<Type, ServiceHost>();

		private static string PtCommunicationProbTitle = "Paratext Communication Problem";
		private static string PtCommunicationProb =
			"The project you are opening will not communicate with Paratext because a project with the same name is " +
			"already open. If you want to use Paratext with this project, make a change in this project" +
			" (so that it will start first), close both projects, then restart Flex.";

		// The different URL prefixes that are required for Windows named pipes and Linux basic http binding.
		// On Linux, just in case port 40001 is in use for something else on a particular system,
		// we allow the user to configure both programs to use a different port.
		internal static string UrlPrefix = Platform.IsWindows
			? "net.pipe://localhost/"
			: $"http://127.0.0.1:{Environment.GetEnvironmentVariable("LEXICAL_PROVIDER_PORT") ?? "40001"}/";

		// Mono requires the pipe handle to use slashes instead of colons.
		// We could put this conditional code somewhere in the routines that generate the pipe handles,
		// but it seemed cleaner to keep all the conditional code for different kinds of pipe more-or-less in one place.
		internal static string FixPipeHandle(string pipeHandle)
		{
			return Platform.IsWindows ? pipeHandle : pipeHandle.Replace(":", "/");
		}

		/// <summary>
		/// Creates a LexicalServiceProvider listener for the specified project.
		/// </summary>
		internal static void StartLexicalServiceProvider(ProjectId projectId, LcmCache cache)
		{
			if (projectId == null)
			{
				throw new InvalidOperationException("Project identity must be known before creating the lexical provider listener");
			}
			var url = UrlPrefix + FixPipeHandle(projectId.PipeHandle);
			StartProvider(new Uri(url), new LexicalServiceProvider(cache), typeof(ILexicalServiceProvider));

			s_lexicalProviderTimer = new Timer(s_timeSinceLexicalProviderUsed_Tick, null, kInactivityTimeout, Timeout.Infinite);
			Logger.WriteEvent("Started listening for lexical service provider requests.");
		}

		/// <summary>
		/// Starts the provider.
		/// </summary>
		private static void StartProvider(Uri providerLocation, object provider, Type providerType)
		{
			if (s_runningProviders.ContainsKey(providerType))
			{
				return;
			}

			var sNamedPipe = providerLocation.ToString();
			// REVIEW: we don't dispose ServiceHost. It might be better to add it to the
			// SingletonsContainer
			ServiceHost providerHost;
			try
			{
				providerHost = new ServiceHost(provider);
				// Named pipes are better for Windows...don't tie up a dedicated port and perform better.
				// However, Mono does not yet support them, so on Mono we use a different binding.
				// Note that any attempt to unify these will require parallel changes in Paratext
				// and some sort of coordinated release of the new versions.

				System.ServiceModel.Channels.Binding binding;
				if (Platform.IsWindows)
				{
					var pipeBinding = new NetNamedPipeBinding
					{
						Security = { Mode = NetNamedPipeSecurityMode.None }
					};
					pipeBinding.MaxBufferSize *= 4;
					pipeBinding.MaxReceivedMessageSize *= 4;
					pipeBinding.MaxBufferPoolSize *= 2;
					pipeBinding.ReaderQuotas.MaxBytesPerRead *= 4;
					pipeBinding.ReaderQuotas.MaxArrayLength *= 4;
					pipeBinding.ReaderQuotas.MaxDepth *= 4;
					pipeBinding.ReaderQuotas.MaxNameTableCharCount *= 4;
					pipeBinding.ReaderQuotas.MaxStringContentLength *= 4;
					binding = pipeBinding;
				}
				else
				{
					var httpBinding = new BasicHttpBinding();
					httpBinding.MaxBufferSize *= 4;
					httpBinding.MaxReceivedMessageSize *= 4;
					httpBinding.MaxBufferPoolSize *= 2;
					httpBinding.ReaderQuotas.MaxBytesPerRead *= 4;
					httpBinding.ReaderQuotas.MaxArrayLength *= 4;
					httpBinding.ReaderQuotas.MaxDepth *= 4;
					httpBinding.ReaderQuotas.MaxNameTableCharCount *= 4;
					httpBinding.ReaderQuotas.MaxStringContentLength *= 4;
					binding = httpBinding;
				}

				providerHost.AddServiceEndpoint(providerType, binding, sNamedPipe);
				providerHost.Open();
			}
			catch (Exception e)
			{
				Logger.WriteError(e);
				if (ScriptureProvider.IsInstalled)
				{
					MessageBox.Show(PtCommunicationProb, PtCommunicationProbTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
				return;
			}
			Logger.WriteEvent($"Started provider {providerLocation} for type {providerType}.");
			s_runningProviders.Add(providerType, providerHost);
		}

		/// <summary>
		/// Resets the lexical provider timer.
		/// </summary>
		private static void ResetLexicalProviderTimer()
		{
			s_lexicalProviderTimer.Change(kInactivityTimeout, Timeout.Infinite);
			FieldWorks.InAppServerMode = true;
		}

		/// <summary>
		/// Releases unmanaged and - optionally - managed resources
		/// </summary>
		internal static void StaticDispose()
		{
			Logger.WriteEvent("Closing service hosts");

			s_lexicalProviderTimer?.Dispose();
			s_lexicalProviderTimer = null;

			foreach (var host in s_runningProviders.Values)
			{
				host.Close();
			}
			s_runningProviders.Clear();
			FieldWorks.InAppServerMode = false; // Make sure FW can shut down
		}

		/// <summary>
		/// Handles the Tick event of the s_timeSinceLexicalProviderUsed control.
		/// </summary>
		private static void s_timeSinceLexicalProviderUsed_Tick(object sender)
		{
			FieldWorks.InAppServerMode = false;
			if (FieldWorks.ProcessCanBeAutoShutDown)
			{
				FieldWorks.GracefullyShutDown();
			}
		}

		/// <summary>
		/// Provides a service contract for getting a lexical provider from an application.
		/// </summary>
		[ServiceBehavior(IncludeExceptionDetailInFaults = true, InstanceContextMode = InstanceContextMode.Single, MaxItemsInObjectGraph = 2147483647)]
		private sealed class LexicalServiceProvider : ILexicalServiceProvider
		{
			/// <summary>String representing the type of the LexicalProvider</summary>
			private const string kLexicalProviderType = "LexicalProvider";
			private const int kSupportedLexicalProviderVersion = 3;
			private readonly LcmCache m_cache;

			/// <summary />
			internal LexicalServiceProvider(LcmCache cache)
			{
				m_cache = cache;
			}

			#region ILexicalServiceProvider Members

			/// <inheritdoc />
			Uri ILexicalServiceProvider.GetProviderLocation(string projhandle, string providerType)
			{
				ResetLexicalProviderTimer();

				if (providerType == kLexicalProviderType)
				{
					var projUri = new Uri($"{LexicalProviderManager.UrlPrefix}{LexicalProviderManager.FixPipeHandle(FwUtils.GeneratePipeHandle($"{projhandle}:LP"))}");
					StartProvider(projUri, new LexicalProviderImpl(m_cache), typeof(ILexicalProvider));
					return projUri;
				}

				return null;
			}

			/// <inheritdoc />
			int ILexicalServiceProvider.GetSupportedVersion(string providerType)
			{
				ResetLexicalProviderTimer();
				return providerType == kLexicalProviderType ? kSupportedLexicalProviderVersion : 0;
			}

			/// <inheritdoc />
			void ILexicalServiceProvider.Ping()
			{
				// Nothing to do for this method except reset our timer for the life of the LexicalProvider.
				// See comment for this method.
				ResetLexicalProviderTimer();
			}

			#endregion

			/// <summary>
			/// This class implements the ILexicalProvider
			/// </summary>
			[ServiceBehavior(IncludeExceptionDetailInFaults = true, InstanceContextMode = InstanceContextMode.Single, MaxItemsInObjectGraph = 2147483647)]
			private sealed class LexicalProviderImpl : ILexicalProvider
			{
				private const string kAnalysisPrefix = "Analysis:";
				private readonly LcmCache m_cache;

				/// <summary />
				internal LexicalProviderImpl(LcmCache cache)
				{
					m_cache = cache;
				}

				#region ILexicalProvider Members

				/// <inheritdoc />
				void ILexicalProvider.ShowEntry(string entry, EntryType entryType)
				{
					ResetLexicalProviderTimer();
					Logger.WriteEvent($"Showing entry from external application for the {entryType} {entry}");
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
						LexEntryUi.DisplayEntries(FieldWorks.Cache, null, new FlexComponentParameters(propertyTable, publisher, subscriber), new FlexHelpTopicProvider(), FwUtilsConstants.UserHelpFile, tss, null);
					});
				}

				/// <inheritdoc />
				void ILexicalProvider.ShowRelatedWords(string entry, EntryType entryType)
				{
					ResetLexicalProviderTimer();
					Logger.WriteEvent($"Showing related word from external application for the {entryType} {entry}");
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
						LexEntryUi.DisplayRelatedEntries(FieldWorks.Cache, null, styleSheet, new FlexHelpTopicProvider(), FwUtilsConstants.UserHelpFile, TsStringUtils.MakeString(entry, FieldWorks.Cache.DefaultVernWs), true);
					});
				}

				/// <inheritdoc />
				IEnumerable<LexicalEntry> ILexicalProvider.Lexemes()
				{
					ResetLexicalProviderTimer();

					// Get all of the lexical entries in the database
					var entries = (m_cache.ServiceLocator.GetInstance<ILexEntryRepository>()
						.AllInstances()
						.Select(dbEntry => new { dbEntry, morphType = dbEntry.PrimaryMorphType })
						.Where(t => t.morphType != null)
						.Select(t => CreateEntryFromDbEntry(GetLexemeTypeForMorphType(t.morphType), t.dbEntry))).ToList();
					// Get all the wordforms in the database
					entries.AddRange(m_cache.ServiceLocator.GetInstance<IWfiWordformRepository>().AllInstances().Select(CreateEntryFromDbWordform));
					return entries;
				}

				/// <inheritdoc />
				LexicalEntry ILexicalProvider.GetLexeme(LexemeType type, string lexicalForm, int homograph)
				{
					ResetLexicalProviderTimer();

					switch (type)
					{
						case LexemeType.Word:
							var wf = GetDbWordform(lexicalForm);
							return wf != null ? CreateEntryFromDbWordform(wf) : null;
						default:
							var dbEntry = GetDbLexeme(type, lexicalForm);
							return dbEntry != null ? CreateEntryFromDbEntry(type, dbEntry) : null;
					}
				}

				/// <inheritdoc />
				void ILexicalProvider.AddLexeme(LexicalEntry lexeme)
				{
					ResetLexicalProviderTimer();
					Logger.WriteEvent($"Adding new lexeme from an external application: {lexeme.LexicalForm}");
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
				LexSense ILexicalProvider.AddSenseToEntry(LexemeType type, string lexicalForm, int homograph)
				{
					ResetLexicalProviderTimer();
					Logger.WriteEvent($"Adding new sense to lexeme '{lexicalForm}' from an external application");
					return NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
					{
						string guid;
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
									var dbEntry = GetDbLexeme(type, lexicalForm);
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
				LexGloss ILexicalProvider.AddGlossToSense(LexemeType type, string lexicalForm, int homograph, string senseId, string language, string text)
				{
					ResetLexicalProviderTimer();

					Logger.WriteEvent($"Adding new gloss to lexeme '{lexicalForm}' from an external application");
					return NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
					{
						IMultiUnicode dbGlosses;
						if (senseId.StartsWith(kAnalysisPrefix))
						{
							// The "sense" is actually an analysis for a wordform and our new
							// gloss is a new meaning for that analysis.
							var dbAnalysis = m_cache.ServiceLocator.GetInstance<IWfiAnalysisRepository>().GetObject(new Guid(senseId.Substring(kAnalysisPrefix.Length)));
							var dbGloss = dbAnalysis.MeaningsOC.FirstOrDefault();
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
							var dbSense = m_cache.ServiceLocator.GetInstance<ILexSenseRepository>().GetObject(new Guid(senseId));
							dbGlosses = dbSense.Gloss;
						}

						// Add the new gloss to the list of glosses for the sense
						var writingSystem = m_cache.WritingSystemFactory.get_Engine(language);
						dbGlosses.set_String(writingSystem.Handle, TsStringUtils.MakeString(text, writingSystem.Handle));

						return new LexGloss(language, text);
					});
				}

				/// <inheritdoc />
				void ILexicalProvider.RemoveGloss(LexemeType type, string lexicalForm, int homograph, string senseId, string language)
				{
					ResetLexicalProviderTimer();

					Logger.WriteEvent($"Removing gloss from lexeme '{lexicalForm}' from an external application");
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
				void ILexicalProvider.Save()
				{
					ResetLexicalProviderTimer();
				}

				/// <inheritdoc />
				void ILexicalProvider.BeginChange()
				{
					ResetLexicalProviderTimer();

					// Ideally, we would like to be able to begin an undo task here and then end it in
					// EndChange(). However, because there is no guarantee that EndChange() will ever
					// get called (and to keep the data in a consistent state), we need to wrap
					// each individual change in it's own undo task.
				}

				/// <inheritdoc />
				void ILexicalProvider.EndChange()
				{
					ResetLexicalProviderTimer();
				}
				#endregion

				#region Private helper methods

				/// <summary>
				/// Gets the DB LexEntry for the specified type and form (homograph currently ignored) or
				/// null if none could be found.
				/// </summary>
				private ILexEntry GetDbLexeme(LexemeType type, string lexicalForm)
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
					m_cache.ServiceLocator.GetInstance<IWfiWordformRepository>().TryGetObject(TsStringUtils.MakeString(lexicalForm, m_cache.DefaultVernWs), true, out var wf);
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
						var tssGloss = glosses.GetStringFromIndex(i, out var ws);
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
							return morphType.IsStemType && morphType.Guid != MoMorphTypeTags.kguidMorphPhrase && morphType.Guid != MoMorphTypeTags.kguidMorphDiscontiguousPhrase;
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
				private static LexemeType GetLexemeTypeForMorphType(IMoMorphType type)
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
					void IPublisher.Publish(PublisherParameterObject publisherParameterObject)
					{
						// Pretend to do something.
					}

					/// <inheritdoc />
					void IPublisher.Publish(IList<PublisherParameterObject> publisherParameterObjects)
					{
						// Pretend to do something.
					}

					#endregion
				}

				/// <summary>
				/// Do nothing ISubscriber, since there are no subscribers in the LexicalProviderImpl.
				/// </summary>
				private sealed class MyDoNothingSubscriber : ISubscriber
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
					IReadOnlyDictionary<string, HashSet<Action<object>>> ISubscriber.Subscriptions => new ReadOnlyDictionary<string, HashSet<Action<object>>>(_subscriptions);

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
						return AsIPropertyRetriever.TryGetValue(name, out propertyValue);
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
	}
}
