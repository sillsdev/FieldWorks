using System;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Paratext.LexicalContracts;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.Utils;

namespace SIL.FieldWorks.ParatextLexiconPlugin
{
	/// <summary>
	/// This is the main Paratext lexicon plugin class
	/// </summary>
	[LexiconPlugin(ID = "FieldWorks", DisplayName = "FieldWorks Language Explorer")]
	public class FwLexiconPlugin : FwDisposableBase, LexiconPlugin
	{
		private const int CacheSize = 5;
		private readonly FdoLexiconCollection m_lexiconCache;
		private readonly FdoCacheCollection m_fdoCacheCache;
		private readonly object m_syncRoot;
		private ActivationContextHelper m_activationContext;

		/// <summary>
		/// Initializes a new instance of the <see cref="FwLexiconPlugin"/> class.
		/// </summary>
		public FwLexiconPlugin()
		{
			m_syncRoot = new object();
			m_lexiconCache = new FdoLexiconCollection();
			m_fdoCacheCache = new FdoCacheCollection();
			m_activationContext = new ActivationContextHelper("FwParatextLexiconPlugin.dll.manifest");

			// initialize client-server services to use Db4O backend for FDO
			var ui = ParatextLexiconFdoUI.Instance;
			var dirs = ParatextLexiconDirectoryFinder.FdoDirectories;
			ClientServerServices.SetCurrentToDb4OBackend(ui, dirs,
				() => dirs.ProjectsDirectory == ParatextLexiconDirectoryFinder.ProjectsDirectoryLocalMachine);
		}

		/// <summary>
		/// Validates the lexical project.
		/// </summary>
		/// <param name="projectId">The project identifier.</param>
		/// <param name="langId">The language identifier.</param>
		/// <returns></returns>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "FdoCache is diposed when the plugin is diposed.")]
		public bool ValidateLexicalProject(string projectId, string langId)
		{
			using (m_activationContext.Activate())
			{
				lock (m_syncRoot)
				{
					FdoCache fdoCache = GetFdoCache(projectId);

					if (fdoCache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems.Any(ws => ws.Id == langId))
						return true;

					DiscardFdoCache(fdoCache);
				}
				return false;
			}
		}

		/// <summary>
		/// Chooses the lexical project.
		/// </summary>
		/// <param name="projectId">The project identifier.</param>
		/// <returns></returns>
		public bool ChooseLexicalProject(out string projectId)
		{
			using (m_activationContext.Activate())
			using (var dialog = new ChooseFdoProjectForm())
			{
				if (dialog.ShowDialog() == DialogResult.OK)
				{
					projectId = dialog.SelectedProject;
					return true;
				}

				projectId = null;
				return false;
			}
		}

		/// <summary>
		/// Gets the lexicon.
		/// </summary>
		/// <param name="scrTextName">Name of the SCR text.</param>
		/// <param name="projectId">The project identifier.</param>
		/// <param name="langId">The language identifier.</param>
		/// <returns></returns>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "FdoLexicon is diposed when the plugin is diposed.")]
		public Lexicon GetLexicon(string scrTextName, string projectId, string langId)
		{
			using (m_activationContext.Activate())
				return GetFdoLexicon(scrTextName, projectId, langId);
		}

		/// <summary>
		/// Gets the word analyses.
		/// </summary>
		/// <param name="scrTextName">Name of the SCR text.</param>
		/// <param name="projectId">The project identifier.</param>
		/// <param name="langId">The language identifier.</param>
		/// <returns></returns>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "FdoLexicon is diposed when the plugin is diposed.")]
		public WordAnalyses GetWordAnalyses(string scrTextName, string projectId, string langId)
		{
			using (m_activationContext.Activate())
				return GetFdoLexicon(scrTextName, projectId, langId);
		}

		private FdoLexicon GetFdoLexicon(string scrTextName, string projectId, string langId)
		{
			lock (m_syncRoot)
			{
				if (m_lexiconCache.Contains(scrTextName))
				{
					FdoLexicon lexicon = m_lexiconCache[scrTextName];
					m_lexiconCache.Remove(scrTextName);
					m_lexiconCache.Insert(0, lexicon);
					return lexicon;
				}

				FdoCache fdoCache = GetFdoCache(projectId);

				IWritingSystem writingSystem;
				if (!fdoCache.ServiceLocator.WritingSystemManager.TryGet(langId, out writingSystem))
					throw new ArgumentException("Could not find a matching vernacular writing system.", "langId");

				if (m_lexiconCache.Count == CacheSize)
				{
					FdoLexicon oldLexicon = m_lexiconCache[CacheSize - 1];
					m_lexiconCache.RemoveAt(CacheSize - 1);

					DiscardFdoCache(oldLexicon.Cache);
				}

				var newLexicon = new FdoLexicon(scrTextName, fdoCache, writingSystem.Handle, m_activationContext);
				m_lexiconCache.Insert(0, newLexicon);
				return newLexicon;
			}
		}

		private FdoCache GetFdoCache(string projectId)
		{
			FdoCache fdoCache;
			if (m_fdoCacheCache.Contains(projectId))
			{
				fdoCache = m_fdoCacheCache[projectId];
			}
			else
			{
				var backendProviderType = FDOBackendProviderType.kSharedXML;
				string path = Path.Combine(ParatextLexiconDirectoryFinder.ProjectsDirectory, projectId, projectId + FdoFileHelper.ksFwDataXmlFileExtension);
				if (!File.Exists(path))
				{
					backendProviderType = FDOBackendProviderType.kDb4oClientServer;
					path = Path.Combine(ParatextLexiconDirectoryFinder.ProjectsDirectory, projectId, projectId + FdoFileHelper.ksFwDataDb4oFileExtension);
					if (!File.Exists(path))
						throw new LexiconUnavailableException("The associated Fieldworks project has been moved, renamed, or does not exist.");
				}

				var progress = new ParatextLexiconThreadedProgress(ParatextLexiconFdoUI.Instance.SynchronizeInvoke) { IsIndeterminate = true, Title = string.Format("Opening {0}", projectId) };
				fdoCache = (FdoCache) progress.RunTask(CreateFdoCache, new ParatextLexiconProjectIdentifier(backendProviderType, path));

				m_fdoCacheCache.Add(fdoCache);
			}
			return fdoCache;
		}

		private static FdoCache CreateFdoCache(IThreadedProgress progress, object[] parameters)
		{
			var projectId = (ParatextLexiconProjectIdentifier) parameters[0];
			try
			{
				return FdoCache.CreateCacheFromExistingData(projectId, Thread.CurrentThread.CurrentUICulture.Name, ParatextLexiconFdoUI.Instance, ParatextLexiconDirectoryFinder.FdoDirectories, progress, true);
			}
			catch (FdoDataMigrationForbiddenException)
			{
				throw new LexiconUnavailableException("The current version of Paratext is not compatible with the associated Fieldworks lexicon.  Please open the lexicon using a compatible version of Fieldworks first.");
			}
			catch (FdoFileLockedException)
			{
				throw new LexiconUnavailableException("Fieldworks and Paratext cannot access the same project at the same time unless the Fieldworks project is shared.");
			}
			catch (StartupException se)
			{
				throw new LexiconUnavailableException(se.Message);
			}
		}

		private void DiscardFdoCache(FdoCache fdoCache)
		{
			if (m_lexiconCache.All(lexicon => lexicon.Cache != fdoCache))
			{
				m_fdoCacheCache.Remove(fdoCache.ProjectId.Name);
				fdoCache.Dispose();
			}
		}

		/// <summary>
		/// Override to dispose managed resources.
		/// </summary>
		protected override void DisposeManagedResources()
		{
			if (m_activationContext != null)
			{
				using (m_activationContext.Activate())
				{
					lock (m_syncRoot)
					{
						foreach (FdoLexicon lexicon in m_lexiconCache)
							lexicon.Dispose();
						m_lexiconCache.Clear();
						foreach (FdoCache fdoCache in m_fdoCacheCache)
							fdoCache.Dispose();
						m_fdoCacheCache.Clear();
					}
				}

				m_activationContext.Dispose();
				m_activationContext = null;
			}
		}

		private class FdoLexiconCollection : KeyedCollection<string, FdoLexicon>
		{
			protected override string GetKeyForItem(FdoLexicon item)
			{
				return item.ScrTextName;
			}
		}

		private class FdoCacheCollection : KeyedCollection<string, FdoCache>
		{
			protected override string GetKeyForItem(FdoCache item)
			{
				return item.ProjectId.Name;
			}
		}
	}
}
