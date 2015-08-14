// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;
using Paratext.LexicalContracts;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.Utils;

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
	public class FwLexiconPlugin : FwDisposableBase, LexiconPlugin
	{
		private const int CacheSize = 5;
		private readonly FdoLexiconCollection m_lexiconCache;
		private readonly FdoCacheCollection m_fdoCacheCache;
		private readonly object m_syncRoot;
		private ActivationContextHelper m_activationContext;
		private readonly ParatextLexiconPluginFdoUI m_ui;

		/// <summary>
		/// Initializes a new instance of the <see cref="FwLexiconPlugin"/> class.
		/// </summary>
		public FwLexiconPlugin()
		{
			// initialize ICU
			RegistryHelper.CompanyName = DirectoryFinder.CompanyName;
			RegistryHelper.ProductName = "FieldWorks";
			Icu.InitIcuDataDir();

			// setup necessary environment variables on Linux
			if (MiscUtils.IsUnix)
			{
				// update ICU_DATA to location of ICU data files
				if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ICU_DATA")))
				{
#if DEBUG
					string icuDataPath = Path.Combine(ParatextLexiconPluginDirectoryFinder.CodeDirectory, "Icu" + Icu.Version);
					if (Directory.Exists(icuDataPath))
						icuDataPath = Icu.DefaultDataDirectory;
#else
					string icuDataPath = Icu.DefaultDataDirectory;
#endif
					Environment.SetEnvironmentVariable("ICU_DATA", icuDataPath);
				}
				// update COMPONENTS_MAP_PATH to point to code directory so that COM objects can be loaded properly
				if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("COMPONENTS_MAP_PATH")))
				{
#if DEBUG
					string compMapPath = Path.GetDirectoryName(FileUtils.StripFilePrefix(Assembly.GetExecutingAssembly().CodeBase));
#else
					string compMapPath = ParatextLexiconPluginDirectoryFinder.CodeDirectory;
#endif
					Environment.SetEnvironmentVariable("COMPONENTS_MAP_PATH", compMapPath);
				}
				// update FW_ROOTCODE so that strings-en.txt file can be found
				if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("FW_ROOTCODE")))
					Environment.SetEnvironmentVariable("FW_ROOTCODE", ParatextLexiconPluginDirectoryFinder.CodeDirectory);
			}

			m_syncRoot = new object();
			m_lexiconCache = new FdoLexiconCollection();
			m_fdoCacheCache = new FdoCacheCollection();
			m_activationContext = new ActivationContextHelper("FwParatextLexiconPlugin.dll.manifest");

			// initialize client-server services to use Db4O backend for FDO
			m_ui = new ParatextLexiconPluginFdoUI(m_activationContext);
			var dirs = ParatextLexiconPluginDirectoryFinder.FdoDirectories;
			ClientServerServices.SetCurrentToDb4OBackend(m_ui, dirs);
		}

		/// <summary>
		/// Validates the lexical project.
		/// </summary>
		/// <param name="projectId">The project identifier.</param>
		/// <param name="langId">The language identifier.</param>
		/// <returns></returns>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "FdoCache is diposed when the plugin is diposed.")]
		public LexicalProjectValidationResult ValidateLexicalProject(string projectId, string langId)
		{
			using (m_activationContext.Activate())
			{
				lock (m_syncRoot)
				{
					FdoCache fdoCache;
					return TryGetFdoCache(projectId, langId, out fdoCache);
				}
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
			using (var dialog = new ChooseFdoProjectForm(m_ui, m_fdoCacheCache))
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
					if (lexicon.ProjectId == projectId)
					{
						m_lexiconCache.Insert(0, lexicon);
						return lexicon;
					}
					DisposeFdoCacheIfUnused(lexicon.Cache);
				}

				FdoCache fdoCache;
				if (TryGetFdoCache(projectId, langId, out fdoCache) != LexicalProjectValidationResult.Success)
					throw new ArgumentException("The specified project is invalid.");

				if (m_lexiconCache.Count == CacheSize)
				{
					FdoLexicon lexicon = m_lexiconCache[CacheSize - 1];
					m_lexiconCache.RemoveAt(CacheSize - 1);
					DisposeFdoCacheIfUnused(lexicon.Cache);
				}

				var newLexicon = new FdoLexicon(scrTextName, projectId, fdoCache, fdoCache.ServiceLocator.WritingSystemManager.GetWsFromStr(langId), m_activationContext);
				m_lexiconCache.Insert(0, newLexicon);
				return newLexicon;
			}
		}

		private LexicalProjectValidationResult TryGetFdoCache(string projectId, string langId, out FdoCache fdoCache)
		{
			fdoCache = null;
			if (string.IsNullOrEmpty(langId))
				return LexicalProjectValidationResult.InvalidLanguage;

			if (m_fdoCacheCache.Contains(projectId))
			{
				fdoCache = m_fdoCacheCache[projectId];
			}
			else
			{
				var backendProviderType = FDOBackendProviderType.kSharedXML;
				string path = Path.Combine(ParatextLexiconPluginDirectoryFinder.ProjectsDirectory, projectId, projectId + FdoFileHelper.ksFwDataXmlFileExtension);
				if (!File.Exists(path))
				{
					backendProviderType = FDOBackendProviderType.kDb4oClientServer;
					path = Path.Combine(ParatextLexiconPluginDirectoryFinder.ProjectsDirectory, projectId, projectId + FdoFileHelper.ksFwDataDb4oFileExtension);
					if (!File.Exists(path))
						return LexicalProjectValidationResult.ProjectDoesNotExist;
				}

				var settings = new FdoSettings {DisableDataMigration = true};
				using (RegistryKey fwKey = ParatextLexiconPluginRegistryHelper.FieldWorksRegistryKeyLocalMachine)
				{
					var sharedXMLBackendCommitLogSize = (int) fwKey.GetValue("SharedXMLBackendCommitLogSize", 0);
					if (sharedXMLBackendCommitLogSize > 0)
						settings.SharedXMLBackendCommitLogSize = sharedXMLBackendCommitLogSize;
				}

				try
				{
					var progress = new ParatextLexiconPluginThreadedProgress(m_ui.SynchronizeInvoke) { IsIndeterminate = true, Title = string.Format("Opening {0}", projectId) };
					fdoCache = FdoCache.CreateCacheFromExistingData(new ParatextLexiconPluginProjectID(backendProviderType, path), Thread.CurrentThread.CurrentUICulture.Name, m_ui,
						ParatextLexiconPluginDirectoryFinder.FdoDirectories, settings, progress);
				}
				catch (FdoDataMigrationForbiddenException)
				{
					return LexicalProjectValidationResult.IncompatibleVersion;
				}
				catch (FdoNewerVersionException)
				{
					return LexicalProjectValidationResult.IncompatibleVersion;
				}
				catch (FdoFileLockedException)
				{
					return LexicalProjectValidationResult.AccessDenied;
				}
				catch (StartupException)
				{
					return LexicalProjectValidationResult.UnknownError;
				}

				m_fdoCacheCache.Add(fdoCache);
			}

			if (fdoCache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems.All(ws => ws.Id != langId))
			{
				DisposeFdoCacheIfUnused(fdoCache);
				fdoCache = null;
				return LexicalProjectValidationResult.InvalidLanguage;
			}

			return LexicalProjectValidationResult.Success;
		}

		private void DisposeFdoCacheIfUnused(FdoCache fdoCache)
		{
			if (m_lexiconCache.All(lexicon => lexicon.Cache != fdoCache))
			{
				m_fdoCacheCache.Remove(fdoCache.ProjectId.Name);
				fdoCache.ServiceLocator.GetInstance<IUndoStackManager>().Save();
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
						{
							fdoCache.ServiceLocator.GetInstance<IUndoStackManager>().Save();
							fdoCache.Dispose();
						}
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
