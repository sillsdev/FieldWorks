// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ServiceModel;

namespace SIL.FieldWorks.WordWorks.Parser
{
	/// <summary>
	/// Thin WCF proxy to the out-of-process HermitCrab worker, replacing HCParser's direct
	/// in-process Morpher calls (RUSTIFY-fieldworks-worker-design.md §2/§3). The net.pipe binding
	/// comes from the shared PipeBindingFactory so this client and the worker's ServiceHost use one
	/// definition.
	///
	/// Owns an HCWorkerProcessManager for spawn/respawn and remembers the last grammar sent so a
	/// mid-call worker crash can be recovered from without the caller re-supplying it: on
	/// CommunicationException/TimeoutException, respawn, replay UpdateGrammar, retry the failed
	/// call once, then surface the error (design §6).
	/// </summary>
	public class HCWorkerClient : IDisposable
	{
		private readonly object m_channelLock = new object();
		private readonly HCWorkerProcessManager m_processManager = new HCWorkerProcessManager();
		private ChannelFactory<IHCWorkerService> m_factory;
		private IHCWorkerService m_channel;
		private HCGrammarDto m_lastGrammar;

		public void UpdateGrammar(string compiledGrammarXml, int deletionReapplications, int maxStemCount, bool mergeEquivalentAnalyses)
		{
			var grammar = new HCGrammarDto
			{
				CompiledGrammarXml = compiledGrammarXml,
				DeletionReapplications = deletionReapplications,
				MaxStemCount = maxStemCount,
				MergeEquivalentAnalyses = mergeEquivalentAnalyses
			};
			CallWithRetry(channel => channel.UpdateGrammar(grammar), grammar);
		}

		public WordAnalysisDto[] ParseWord(string word, bool guessRoots)
		{
			WordAnalysisDto[] result = null;
			CallWithRetry(channel => result = channel.ParseWord(word, guessRoots), m_lastGrammar);
			return result;
		}

		public IDictionary<string, WordAnalysisDto[]> ParseWordsBatch(string[] words, bool guessRoots)
		{
			IDictionary<string, WordAnalysisDto[]> result = null;
			CallWithRetry(channel => result = channel.ParseWordsBatch(words, guessRoots), m_lastGrammar);
			return result;
		}

		/// <summary>
		/// Kills the worker process (FieldWorks exit, or an idle timeout - design §4). The next
		/// call after this lazily respawns and replays UpdateGrammar, same as a crash recovery.
		/// </summary>
		public void Shutdown()
		{
			lock (m_channelLock)
			{
				CloseChannel();
				m_processManager.Shutdown();
			}
		}

		private void CallWithRetry(Action<IHCWorkerService> call, HCGrammarDto grammarToReplay)
		{
			IHCWorkerService channel = GetOrCreateChannel();
			try
			{
				call(channel);
				// UpdateGrammar itself succeeded - remember it for a future respawn's replay.
				// (Assigning unconditionally here is harmless when grammarToReplay is m_lastGrammar
				// itself, e.g. from ParseWord/ParseWordsBatch.)
				if (grammarToReplay != null)
					m_lastGrammar = grammarToReplay;
			}
			catch (Exception e) when (e is CommunicationException || e is TimeoutException)
			{
				// Worker crashed or the pipe is otherwise unusable: respawn, replay the grammar
				// (idempotent - design §6), and retry the failed call exactly once before
				// surfacing the error to the caller/UI.
				lock (m_channelLock)
				{
					CloseChannel();
				}
				IHCWorkerService retryChannel = GetOrCreateChannel();
				if (m_lastGrammar != null)
					retryChannel.UpdateGrammar(m_lastGrammar);
				call(retryChannel);
				if (grammarToReplay != null)
					m_lastGrammar = grammarToReplay;
			}
		}

		private IHCWorkerService GetOrCreateChannel()
		{
			lock (m_channelLock)
			{
				if (m_channel != null)
					return m_channel;

				string pipeName = m_processManager.EnsureStarted();
				// One binding definition shared with the worker's ServiceHost (both sides must agree
				// on quotas/timeouts).
				NetNamedPipeBinding pipeBinding = PipeBindingFactory.Create();

				m_factory = new ChannelFactory<IHCWorkerService>(
					pipeBinding,
					new EndpointAddress("net.pipe://localhost/" + pipeName));
				m_channel = m_factory.CreateChannel();
				return m_channel;
			}
		}

		private void CloseChannel()
		{
			lock (m_channelLock)
			{
				try
				{
					(m_channel as ICommunicationObject)?.Abort();
				}
				catch (Exception)
				{
					// Best-effort teardown of a channel we already know is broken.
				}
				m_factory?.Abort();
				m_channel = null;
				m_factory = null;
			}
		}

		public void Dispose()
		{
			Shutdown();
		}
	}
}
