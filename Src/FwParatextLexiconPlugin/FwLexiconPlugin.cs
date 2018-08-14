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
using SIL.WritingSystems;

namespace SIL.FieldWorks.ParatextLexiconPlugin
{
	/// <summary>
	/// This is the main Paratext lexicon plugin.
	///
	/// It uses an activation context to load the required COM objects. The activation context should be activated
	/// when making any calls to LCM to ensure that COM objects can be loaded properly. Care should be taken to ensure
	/// that no calls to LCM occur outside of an activated activation context. The easiest way to do this is to ensure
	/// that the activation context is activated in all public methods of all implemented interfaces. Be careful of
	/// deferred execution enumerables, such as those used in LINQ and yield statements. The best way to avoid deferred
	/// execution of enumerables is to call "ToArray()" or something equivalent when returning the results of LINQ
	/// functions. Do not use yield statements, instead add all objects to a collection and return the collection.
	/// </summary>
	[LexiconPlugin(ID = "FieldWorks", DisplayName = "FieldWorks Language Explorer")]
	public class FwLexiconPlugin : DisposableBase, LexiconPlugin
	{
		private const int CacheSize = 5;
		private readonly LcmLexiconCollection m_lexiconCache;
		private readonly LcmCacheCollection m_cacheCache;
		private readonly object m_syncRoot;
		private readonly ParatextLexiconPluginLcmUI m_ui;

		/// <summary>
		/// Initializes a new instance of the <see cref="FwLexiconPlugin"/> class.
		/// </summary>
		public FwLexiconPlugin()
		{
			FwRegistryHelper.Initialize();

			// setup necessary environment variables on Linux
			if (MiscUtils.IsUnix)
			{
				// update ICU_DATA to location of ICU data files
				if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ICU_DATA")))
				{
					string codeIcuDataPath = Path.Combine(ParatextLexiconPluginDirectoryFinder.CodeDirectory, "Icu" + Icu.Version);
#if DEBUG
					string icuDataPath = codeIcuDataPath;
#else
					string icuDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), ".config/fieldworks/Icu" + Icu.Version);
					if (!Directory.Exists(icuDataPath))
						icuDataPath = codeIcuDataPath;
#endif
					Environment.SetEnvironmentVariable("ICU_DATA", icuDataPath);
				}
				// update COMPONENTS_MAP_PATH to point to code directory so that COM objects can be loaded properly
				if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("COMPONENTS_MAP_PATH")))
				{
					string compMapPath = Path.GetDirectoryName(FileUtils.StripFilePrefix(Assembly.GetExecutingAssembly().CodeBase));
					Environment.SetEnvironmentVariable("COMPONENTS_MAP_PATH", compMapPath);
				}
				// update FW_ROOTCODE so that strings-en.txt file can be found
				if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("FW_ROOTCODE")))
					Environment.SetEnvironmentVariable("FW_ROOTCODE", ParatextLexiconPluginDirectoryFinder.CodeDirectory);
			}
			FwUtils.InitializeIcu();
			if (!Sldr.IsInitialized)
			{
				Sldr.Initialize();
			}

			m_syncRoot = new object();
			m_lexiconCache = new LcmLexiconCollection();
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
				LcmCache lcmCache;
				return TryGetLcmCache(projectId, langId, out lcmCache);
			}
		}

		/// <summary>
		/// Chooses the lexical project.
		/// </summary>
		/// <param name="projectId">The project identifier.</param>
		/// <returns></returns>
		public bool ChooseLexicalProject(out string projectId)
		{
			using (var dialog = new ChooseLcmProjectForm(m_ui, m_cacheCache))
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
			return GetLcmLexicon(scrTextName, projectId, langId);
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
			return GetLcmLexicon(scrTextName, projectId, langId);
		}

		private LcmLexicon GetLcmLexicon(string scrTextName, string projectId, string langId)
		{
			lock (m_syncRoot)
			{
				if (m_lexiconCache.Contains(scrTextName))
				{
					LcmLexicon lexicon = m_lexiconCache[scrTextName];
					m_lexiconCache.Remove(scrTextName);
					if (lexicon.ProjectId == projectId)
					{
						m_lexiconCache.Insert(0, lexicon);
						return lexicon;
					}
					DisposeLcmCacheIfUnused(lexicon.Cache);
				}

				LcmCache lcmCache;
				if (TryGetLcmCache(projectId, langId, out lcmCache) != LexicalProjectValidationResult.Success)
					throw new ArgumentException("The specified project is invalid.");

				if (m_lexiconCache.Count == CacheSize)
				{
					LcmLexicon lexicon = m_lexiconCache[CacheSize - 1];
					m_lexiconCache.RemoveAt(CacheSize - 1);
					DisposeLcmCacheIfUnused(lexicon.Cache);
				}

				var newLexicon = new LcmLexicon(scrTextName, projectId, lcmCache, lcmCache.ServiceLocator.WritingSystemManager.GetWsFromStr(langId));
				m_lexiconCache.Insert(0, newLexicon);
				return newLexicon;
			}
		}

		private LexicalProjectValidationResult TryGetLcmCache(string projectId, string langId, out LcmCache lcmCache)
		{
			lcmCache = null;
			if (string.IsNullOrEmpty(langId))
				return LexicalProjectValidationResult.InvalidLanguage;

			if (m_cacheCache.Contains(projectId))
			{
				lcmCache = m_cacheCache[projectId];
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
					lcmCache = LcmCache.CreateCacheFromExistingData(lcmProjectId, Thread.CurrentThread.CurrentUICulture.Name, m_ui,
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

				m_cacheCache.Add(lcmCache);
			}

			if (lcmCache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems.All(ws => ws.Id != langId))
			{
				DisposeLcmCacheIfUnused(lcmCache);
				lcmCache = null;
				return LexicalProjectValidationResult.InvalidLanguage;
			}

			return LexicalProjectValidationResult.Success;
		}

		private void DisposeLcmCacheIfUnused(LcmCache lcmCache)
		{
			if (m_lexiconCache.All(lexicon => lexicon.Cache != lcmCache))
			{
				m_cacheCache.Remove(lcmCache.ProjectId.Name);
				lcmCache.ServiceLocator.GetInstance<IUndoStackManager>().Save();
				lcmCache.Dispose();
			}
		}

		/// <summary>
		/// Override to dispose managed resources.
		/// </summary>
		protected override void DisposeManagedResources()
		{
			lock (m_syncRoot)
			{
				foreach (LcmLexicon lexicon in m_lexiconCache)
					lexicon.Dispose();
				m_lexiconCache.Clear();
				foreach (LcmCache lcmCache in m_cacheCache)
				{
					lcmCache.ServiceLocator.GetInstance<IUndoStackManager>().Save();
					lcmCache.Dispose();
				}
				m_cacheCache.Clear();
			}

			Sldr.Cleanup();
		}

		private class LcmLexiconCollection : KeyedCollection<string, LcmLexicon>
		{
			protected override string GetKeyForItem(LcmLexicon item)
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
