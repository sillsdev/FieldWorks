// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;
using Paratext.LexicalContracts;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel.Core.Text;
using SIL.LCModel;
using SIL.ObjectModel;
using SIL.LCModel.Utils;
using SIL.PlatformUtilities;
using SIL.WritingSystems;

namespace SIL.FieldWorks.ParatextLexiconPlugin
{
	/// <summary>
	/// This is the main Paratext lexicon plugin.
	///
	/// It uses an activation context to load the required COM objects. The activation context should be activated
	/// when making any calls to FDO to ensure that COM objects can be loaded properly. Care should be taken to ensure
	/// that no calls to FDO occur outside of an activated activation context. The easiest way to do this is to ensure
	/// that the activation context is activated in all public methods of all implemented interfaces. Be careful of
	/// deferred execution enumerables, such as those used in LINQ and yield statements. The best way to avoid deferred
	/// execution of enumerables is to call "ToArray()" or something equivalent when returning the results of LINQ
	/// functions. Do not use yield statements, instead add all objects to a collection and return the collection.
	/// </summary>
	[LexiconPlugin(ID = "FieldWorks", DisplayName = "FieldWorks Language Explorer")]
	public class FwLexiconPlugin : DisposableBase, LexiconPlugin, LexiconPluginV2
	{
		private const int CacheSize = 5;
		private readonly FdoLexiconCollection m_lexiconCache;
		private readonly LcmCacheCollection m_cacheCache;
		private readonly object m_syncRoot;
		private readonly ParatextLexiconPluginLcmUI m_ui;

		/// <summary>
		/// Initializes a new instance of the <see cref="FwLexiconPlugin"/> class.
		/// </summary>
		public FwLexiconPlugin()
		{
			FwRegistryHelper.Initialize();

			FwUtils.InitializeIcu();
			if (!Sldr.IsInitialized)
			{
				Sldr.Initialize();
			}

			m_syncRoot = new object();
			m_lexiconCache = new FdoLexiconCollection();
			m_cacheCache = new LcmCacheCollection();
			m_ui = new ParatextLexiconPluginLcmUI();
		}

		/// <summary>
		/// Validates the lexical project.
		/// </summary>
		/// <param name="projectId">The project identifier.</param>
		/// <param name="langId">The language identifier.</param>
		/// <returns></returns>
		public LexicalProjectValidationResult ValidateLexicalProject(string projectId, string langId)
		{
			lock (m_syncRoot)
			{
				LcmCache fdoCache;
				return TryGetLcmCache(projectId, langId, out fdoCache);
			}
		}

		LexiconV2 LexiconPluginV2.GetLexicon(string scrTextName, string projectId, string langId)
		{
			return GetFdoLexicon(scrTextName, projectId, langId);
		}

		/// <summary>
		/// Chooses the lexical project.
		/// </summary>
		/// <param name="projectId">The project identifier.</param>
		/// <returns></returns>
		public bool ChooseLexicalProject(out string projectId)
		{
			using (var dialog = new ChooseFdoProjectForm(m_ui, m_cacheCache))
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
		public Lexicon GetLexicon(string scrTextName, string projectId, string langId)
		{
			return GetFdoLexicon(scrTextName, projectId, langId);
		}

		/// <summary>
		/// Gets the word analyses.
		/// </summary>
		/// <param name="scrTextName">Name of the SCR text.</param>
		/// <param name="projectId">The project identifier.</param>
		/// <param name="langId">The language identifier.</param>
		/// <returns></returns>
		public WordAnalyses GetWordAnalyses(string scrTextName, string projectId, string langId)
		{
			return GetFdoLexicon(scrTextName, projectId, langId);
		}

		WordAnalysesV2 LexiconPluginV2.GetWordAnalyses(string scrTextName, string projectId, string langId)
		{
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
					if (lexicon.ProjectId == projectId)
					{
						m_lexiconCache.Insert(0, lexicon);
						return lexicon;
					}
					DisposeLcmCacheIfUnused(lexicon.Cache);
				}

				LcmCache fdoCache;
				if (TryGetLcmCache(projectId, langId, out fdoCache) != LexicalProjectValidationResult.Success)
					throw new ArgumentException("The specified project is invalid.");

				if (m_lexiconCache.Count == CacheSize)
				{
					FdoLexicon lexicon = m_lexiconCache[CacheSize - 1];
					m_lexiconCache.RemoveAt(CacheSize - 1);
					DisposeLcmCacheIfUnused(lexicon.Cache);
				}

				var newLexicon = new FdoLexicon(scrTextName, projectId, fdoCache, fdoCache.ServiceLocator.WritingSystemManager.GetWsFromStr(langId));
				m_lexiconCache.Insert(0, newLexicon);
				return newLexicon;
			}
		}

		private LexicalProjectValidationResult TryGetLcmCache(string projectId, string langId, out LcmCache fdoCache)
		{
			fdoCache = null;
			if (string.IsNullOrEmpty(langId))
				return LexicalProjectValidationResult.InvalidLanguage;

			if (m_cacheCache.Contains(projectId))
			{
				fdoCache = m_cacheCache[projectId];
			}
			else
			{
				var path = Path.Combine(ParatextLexiconPluginDirectoryFinder.ProjectsDirectory, projectId, projectId + LcmFileHelper.ksFwDataXmlFileExtension);
				if (!File.Exists(path))
				{
					return LexicalProjectValidationResult.ProjectDoesNotExist;
				}

				var settings = new LcmSettings {DisableDataMigration = true};
				using (RegistryKey fwKey = ParatextLexiconPluginRegistryHelper.FieldWorksRegistryKeyLocalMachine)
				{
					if (fwKey != null)
					{
						var sharedXMLBackendCommitLogSize = (int) fwKey.GetValue("SharedXMLBackendCommitLogSize", 0);
						if (sharedXMLBackendCommitLogSize > 0)
							settings.SharedXMLBackendCommitLogSize = sharedXMLBackendCommitLogSize;
					}
				}

				try
				{
					var progress = new ParatextLexiconPluginThreadedProgress(m_ui.SynchronizeInvoke)
					{
						IsIndeterminate = true,
						Title = $"Opening {projectId}"
					};
					var lcmProjectId = new ParatextLexiconPluginProjectId(BackendProviderType.kSharedXML, path);
					fdoCache = LcmCache.CreateCacheFromExistingData(lcmProjectId, Thread.CurrentThread.CurrentUICulture.Name, m_ui,
						ParatextLexiconPluginDirectoryFinder.LcmDirectories, settings, progress);
				}
				catch (LcmDataMigrationForbiddenException)
				{
					return LexicalProjectValidationResult.IncompatibleVersion;
				}
				catch (LcmNewerVersionException)
				{
					return LexicalProjectValidationResult.IncompatibleVersion;
				}
				catch (LcmFileLockedException)
				{
					return LexicalProjectValidationResult.AccessDenied;
				}
				catch (LcmInitializationException)
				{
					return LexicalProjectValidationResult.UnknownError;
				}

				m_cacheCache.Add(fdoCache);
			}

			if (fdoCache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems.All(ws => ws.Id != langId))
			{
				DisposeLcmCacheIfUnused(fdoCache);
				fdoCache = null;
				return LexicalProjectValidationResult.InvalidLanguage;
			}

			return LexicalProjectValidationResult.Success;
		}

		private void DisposeLcmCacheIfUnused(LcmCache fdoCache)
		{
			if (m_lexiconCache.All(lexicon => lexicon.Cache != fdoCache))
			{
				m_cacheCache.Remove(fdoCache.ProjectId.Name);
				fdoCache.ServiceLocator.GetInstance<IUndoStackManager>().Save();
				fdoCache.Dispose();
			}
		}

		/// <summary>
		/// Override to dispose managed resources.
		/// </summary>
		protected override void DisposeManagedResources()
		{
			lock (m_syncRoot)
			{
				foreach (FdoLexicon lexicon in m_lexiconCache)
					lexicon.Dispose();
				m_lexiconCache.Clear();
				foreach (LcmCache fdoCache in m_cacheCache)
				{
					fdoCache.ServiceLocator.GetInstance<IUndoStackManager>().Save();
					fdoCache.Dispose();
				}
				m_cacheCache.Clear();
			}

			Sldr.Cleanup();
		}

		private class FdoLexiconCollection : KeyedCollection<string, FdoLexicon>
		{
			protected override string GetKeyForItem(FdoLexicon item)
			{
				return item.ScrTextName;
			}
		}

		private class LcmCacheCollection : KeyedCollection<string, LcmCache>
		{
			protected override string GetKeyForItem(LcmCache item)
			{
				return item.ProjectId.Name;
			}
		}
	}
}
