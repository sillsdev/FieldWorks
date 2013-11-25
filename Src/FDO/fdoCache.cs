// Copyright (c) 2002-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: fdoCache.cs
// Responsibility: Randy Regnier

using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Xml.Linq;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.Application.ApplicationServices;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.IOC;
using SIL.FieldWorks.Resources;
using SIL.Utils;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO.Infrastructure.Impl;
using SIL.Utils.FileDialog;

namespace SIL.FieldWorks.FDO
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// The FdoCache is not really a "cache". It provides for a shortcut for accessing certain
	/// common things associated with a particular project, including the service locator, which
	/// provides access to repositories of all CmObjects. Only one CmObject exists per Guid/Hvo,
	/// so any FDO clients always get the exact same one, when the Guid and Hvo are the same.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public sealed partial class FdoCache
	{
		#region Data Members
		private ThreadHelper m_threadHelper; // FdoCache is NOT responsible to dispose this (typically FieldWorks.s_threadHelper)
		private IFdoServiceLocator m_serviceLocator;
		private static object m_syncRoot = new object();

		/// <summary>
		/// This is the 'hvo' for null values. (This is not very identifiable, but unfortunately, a lot of code (e.g., in Views) knows that
		/// 0 means an empty atomic property.
		/// </summary>
		public static readonly int kNullHvo = 0;

		/// <summary>
		/// This dictionary is used to implement custom properties. Locate a particular property by making a Tuple from
		/// the CmObject and the flid. To find all the properties for an object, it is necessary to use the MDC to
		/// find all relevant flids.
		/// </summary>
		internal readonly Dictionary<Tuple<ICmObject, int>, object> CustomProperties = new Dictionary<Tuple<ICmObject, int>, object>();

		private readonly HashSet<ICmObject> m_objectsBeingDeleted = new HashSet<ICmObject>();

		private ILgWritingSystemFactory m_lgwsFactory;
		#endregion Data Members

		#region Delegates and Events
		/// <summary>Delegate declaration for handling the ProjectNameChanged event</summary>
		/// <param name="sender">The cache that raised the event</param>
		public delegate void ProjectNameChangedHandler(FdoCache sender);
		/// <summary>Event raised when the project DB name is changed</summary>
		public event ProjectNameChangedHandler ProjectNameChanged;
		/// <summary>Delegate declaration for when a newer writing system is found in the
		/// global writing system repository</summary>
		/// <param name="wsLabel">The display name of the writing system</param>
		/// <param name="projectName">the name of the project that has the out-of-date writing system.</param>
		/// <returns>True if the writing system should be refreshed, false to ignore the
		/// updated writing system</returns>
		public delegate bool NewerWritingSystemFoundHandler(string wsLabel, string projectName);
		/// <summary>Event raised when a newer writing system is found in the global writing
		/// system repository</summary>
		public static event NewerWritingSystemFoundHandler NewerWritingSystemFound;
		#endregion

		#region Creation/initialization methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new FdoCache that uses a specified data provider type with no language
		/// project.
		/// </summary>
		/// <param name="projectId">Identifies the new project to create.</param>
		/// <param name="userWsIcuLocale">The ICU locale of the default user WS.</param>
		/// <param name="threadHelper">The thread helper used for invoking actions on the main
		/// UI thread.</param>
		/// ------------------------------------------------------------------------------------
		public static FdoCache CreateCacheWithNoLangProj(IProjectIdentifier projectId,
			string userWsIcuLocale, ThreadHelper threadHelper)
		{
			FdoCache createdCache = CreateCacheInternal(projectId, threadHelper);
			createdCache.FullyInitializedAndReadyToRock = true;
			return createdCache;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new FdoCache that uses a specified data provider type that creates a new,
		/// blank language project.
		/// </summary>
		/// <remarks>This should probably only be used in tests (currently also in one place in
		/// the FDO browser that really isn't used). Could theoretically be used to create a
		/// new cache without all the nice useful suff in NewLangProj, but why bother?</remarks>
		/// <param name="projectId">Identifies the new project to create.</param>
		/// <param name="analWsIcuLocale">The ICU locale of the default analysis writing
		/// system.</param>
		/// <param name="vernWsIcuLocale">The ICU locale of the default vernacular writing
		/// system.</param>
		/// <param name="userWsIcuLocale">The ICU locale of the default user writing
		/// system.</param>
		/// <param name="threadHelper">The thread helper used for invoking actions on the main
		/// UI thread.</param>
		/// ------------------------------------------------------------------------------------
		public static FdoCache CreateCacheWithNewBlankLangProj(IProjectIdentifier projectId,
			string analWsIcuLocale, string vernWsIcuLocale, string userWsIcuLocale,
			ThreadHelper threadHelper)
		{
			FdoCache createdCache = CreateCacheInternal(projectId, userWsIcuLocale, threadHelper,
				dataSetup => dataSetup.CreateNewLanguageProject(projectId));
			NonUndoableUnitOfWorkHelper.Do(createdCache.ActionHandlerAccessor, () =>
			{
				createdCache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem =
					createdCache.ServiceLocator.WritingSystemManager.Get(analWsIcuLocale);
				createdCache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem =
					createdCache.ServiceLocator.WritingSystemManager.Get(vernWsIcuLocale);
			});
			return createdCache;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new FdoCache that uses a specified data provider type that loads the
		/// language project data from an existing source.
		/// </summary>
		/// <param name="projectId">Identifies the project to load.</param>
		/// <param name="userWsIcuLocale">The ICU locale of the default user WS.</param>
		/// <param name="progressDlg">The progress dialog box</param>
		/// ------------------------------------------------------------------------------------
		public static FdoCache CreateCacheFromExistingData(IProjectIdentifier projectId,
			string userWsIcuLocale, IThreadedProgress progressDlg)
		{
			return CreateCacheInternal(projectId, userWsIcuLocale, progressDlg.ThreadHelper,
				dataSetup => dataSetup.StartupExtantLanguageProject(projectId, true, progressDlg),
				cache => cache.Initialize());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new FdoCache from a local file. This is typically a temporary cache
		/// (e.g., used to import lift when obtaining from a new repo).
		/// </summary>
		/// <param name="projectPath"></param>
		/// <param name="userWsIcuLocale"></param>
		/// <param name="progressDlg">The progress dialog box</param>
		/// ------------------------------------------------------------------------------------
		public static FdoCache CreateCacheFromLocalProjectFile(string projectPath,
			string userWsIcuLocale, IThreadedProgress progressDlg)
		{
			var projectId = new SimpleProjectId(FDOBackendProviderType.kXML, projectPath);
			return CreateCacheInternal(projectId, userWsIcuLocale, progressDlg.ThreadHelper,
				dataSetup => dataSetup.StartupExtantLanguageProject(projectId, true, progressDlg),
				cache => cache.Initialize());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new FdoCache that uses a specified data provider type that loads the
		/// language project data from another FdoCache.
		/// </summary>
		/// <param name="projectId">Identifies the project to create (i.e., the copy).</param>
		/// <param name="userWsIcuLocale">The ICU locale of the default user WS.</param>
		/// <param name="sourceCache">The FdoCache to copy</param>
		/// <param name="threadHelper">The thread helper used for invoking actions on the main
		/// UI thread.</param>
		/// ------------------------------------------------------------------------------------
		public static FdoCache CreateCacheCopy(IProjectIdentifier projectId,
			string userWsIcuLocale, FdoCache sourceCache, ThreadHelper threadHelper)
		{
			return CreateCacheInternal(projectId, userWsIcuLocale, threadHelper,
				dataSetup => dataSetup.InitializeFromSource(projectId, sourceCache));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new FdoCache that uses a specified data provider.
		/// </summary>
		/// <param name="projectId">Identifies the project to create or load.</param>
		/// <param name="threadHelper">The thread helper used for invoking actions on the main
		/// UI thread.</param>
		/// ------------------------------------------------------------------------------------
		private static FdoCache CreateCacheInternal(IProjectIdentifier projectId,
			ThreadHelper threadHelper)
		{
			FDOBackendProviderType providerType = projectId.Type;
			if (providerType == FDOBackendProviderType.kXMLWithMemoryOnlyWsMgr)
				providerType = FDOBackendProviderType.kXML;

			var iocFactory = new FdoServiceLocatorFactory(providerType);
			var servLoc = (IFdoServiceLocator)iocFactory.CreateServiceLocator();
			var createdCache = servLoc.GetInstance<FdoCache>();
			createdCache.m_serviceLocator = servLoc;
			createdCache.m_lgwsFactory = servLoc.GetInstance<ILgWritingSystemFactory>();
			createdCache.m_threadHelper = threadHelper;
			return createdCache;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new FdoCache that uses a specified data provider.
		/// </summary>
		/// <param name="projectId">Identifies the project to create or load.</param>
		/// <param name="userWsIcuLocale">The ICU locale of the default user WS.</param>
		/// <param name="threadHelper">The thread helper used for invoking actions on the main
		/// UI thread.</param>
		/// <param name="doThe">The data setup action to perform.</param>
		/// ------------------------------------------------------------------------------------
		private static FdoCache CreateCacheInternal(IProjectIdentifier projectId,
			string userWsIcuLocale, ThreadHelper threadHelper, Action<IDataSetup> doThe)
		{
			return CreateCacheInternal(projectId, userWsIcuLocale, threadHelper, doThe, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new FdoCache that uses a specified data provider.
		/// </summary>
		/// <param name="projectId">Identifies the project to create or load.</param>
		/// <param name="userWsIcuLocale">The ICU locale of the default user WS.</param>
		/// <param name="threadHelper">The thread helper used for invoking actions on the main
		/// UI thread.</param>
		/// <param name="doThe">The data setup action to perform.</param>
		/// <param name="initialize">The initialization step to perfrom (can be null).</param>
		/// <returns>The newly created cache</returns>
		/// ------------------------------------------------------------------------------------
		private static FdoCache CreateCacheInternal(IProjectIdentifier projectId,
			string userWsIcuLocale, ThreadHelper threadHelper, Action<IDataSetup> doThe,
			Action<FdoCache> initialize)
		{
			FDOBackendProviderType providerType = projectId.Type;
			bool useMemoryWsManager = (providerType == FDOBackendProviderType.kMemoryOnly ||
				providerType == FDOBackendProviderType.kXMLWithMemoryOnlyWsMgr);
			FdoCache createdCache = CreateCacheInternal(projectId, threadHelper);

			// Init backend data provider
			IDataSetup dataSetup = createdCache.ServiceLocator.GetInstance<IDataSetup>();
			dataSetup.UseMemoryWritingSystemManager = useMemoryWsManager;
			doThe(dataSetup);
			if (initialize != null)
				initialize(createdCache);

			createdCache.FullyInitializedAndReadyToRock = true;

			// Set the default user ws if we know one.
			// This is especially important because (as of 12 Feb 2008) we are not localizing
			// the resource string in the Language DLL which controls the default UI language.
			if (!string.IsNullOrEmpty(userWsIcuLocale))
			{
				IFdoServiceLocator servLoc = createdCache.ServiceLocator;
				IWritingSystem wsUser;
				servLoc.WritingSystemManager.GetOrSet(userWsIcuLocale, out wsUser);
				servLoc.WritingSystemManager.UserWritingSystem = wsUser;
			}

			createdCache.EnsureValidLinkedFilesFolder();
			return createdCache;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes this cache (called whenever the cache is created).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void Initialize()
		{
			// check for global writing system updates
			var writingSystemManager = ServiceLocator.WritingSystemManager;
			var newerGlobalWritingSystems = writingSystemManager.CheckForNewerGlobalWritingSystems().ToList();

			// If there are updates, and at least one of the following is true:
			// - The user has cleared the "Do Not Copy Writing Systems to Other Projects" box (cleared by default)
			// - The NewerWritingSystemFound method is null
			// - The NewerWritingSystemFound method returns true (the user wants to copy these WS's from global this time)
			if (newerGlobalWritingSystems.Any() && (
				CoreImpl.Properties.Settings.Default.UpdateGlobalWSStore ||
				NewerWritingSystemFound == null ||
				NewerWritingSystemFound(WsNamesToString(newerGlobalWritingSystems), ProjectId.UiName)))
			{
				SaveOnlyLocalWritingSystems(writingSystemManager, newerGlobalWritingSystems);
			}

			if (ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem == null)
			{
				throw new FwStartupException(ResourceHelper.FormatResourceString("kstidNoWritingSystems",
					ResourceHelper.GetResourceString("kstidAnalysis")));
			}
			if (ServiceLocator.WritingSystems.DefaultVernacularWritingSystem == null)
			{
				throw new FwStartupException(ResourceHelper.FormatResourceString("kstidNoWritingSystems",
					ResourceHelper.GetResourceString("kstidVernacular")));
			}

			NonUndoableUnitOfWorkHelper.Do(ActionHandlerAccessor, () =>
				DataStoreInitializationServices.PrepareCache(this));
		}

		private static string WsNamesToString(IEnumerable<IWritingSystem> writingSystems)
		{
			var result = new StringBuilder();
			foreach (var ws in writingSystems)
			{
				if (result.Length > 0)
					result.Append(", ");
				result.Append(ws.DisplayLabel);
			}
			return result.ToString();
		}

		private static void SaveOnlyLocalWritingSystems(IWritingSystemManager writingSystemManager, IEnumerable<IWritingSystem> writingSystems)
		{
			foreach (var ws in writingSystems)
			{
				// We only copied changes from Global, so the WS has not been "modified."
				// Setting Modified to false prevents bumping the date on the global WS.
				// Running Replace ensures the local ldml will be saved regardless of the Modified flag.
				writingSystemManager.Replace(ws);
				ws.Modified = false;
			}
			writingSystemManager.Save();
		}

		#endregion

		#region Methods for creating a new language project
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new language project for the new language name
		/// </summary>
		/// <param name="progressDlg">The progress dialog.</param>
		/// <param name="parameters">One or more parameters, supplied in this order:
		///  0. A string containing the name of the project (required);
		///  1. A ThreadHelper used for invoking actions on the main UI thread (required);
		///  2. An IWritingSystem to be used as the default analylis writing system (default: English);
		///  3. An IWritingSystem to be used as the default vernacular writing system (default: French);
		///  4. A string with the ICU locale of the UI writing system (default: "en").
		///  5. A set of IWritingSystem to provide additional analysis writing systems (default: no more)
		///  6. A set of IWritingSystem to provide additional vernacular writing systems (default: no more)
		///  7. OCM Data filename. (default: OCM-Frame.xml if available; else, null)</param>
		/// <returns>Path of the newly created project file.</returns>
		/// <remarks>Override DisplayUi to prevent progress dialog from showing.</remarks>
		/// ------------------------------------------------------------------------------------
		public static string CreateNewLangProj(IThreadedProgress progressDlg, params object[] parameters)
		{
			if (parameters == null || parameters.Length < 2)
				throw new ArgumentException("Parameters must include at least a project name and the ThreadHelper");
			var projectName = (string)parameters[0];
			if (string.IsNullOrEmpty(projectName))
				throw new ArgumentNullException("projectName", "Cannot be null or empty");
			var threadHelper = (ThreadHelper)parameters[1];
			IWritingSystem analWrtSys = (parameters.Length > 2) ? (IWritingSystem)parameters[2] : null;
			IWritingSystem vernWrtSys = (parameters.Length > 3) ? (IWritingSystem)parameters[3] : null;
			var userIcuLocale = (parameters.Length > 4 && parameters[4] != null) ? (string)parameters[4] : "en";
			const int nMax = 10;
			if (progressDlg != null)
			{
				progressDlg.Minimum = 0;
				progressDlg.Maximum = nMax;
				progressDlg.Message = Properties.Resources.kstidCreatingDB;
			}

			var dbFileName = CreateNewDbFile(ref projectName);
			Debug.Assert(DirectoryFinder.IsSubFolderOfProjectsDirectory(Path.GetDirectoryName(dbFileName)),
				"new projects should always be created in the current projects directory");

			if (progressDlg != null)
			{
				progressDlg.Step(0);
				progressDlg.Message = Properties.Resources.kstidInitializingDB;
			}

			var projectId = new SimpleProjectId(FDOBackendProviderType.kXML, dbFileName);
			using (var cache = CreateCacheInternal(projectId,
				userIcuLocale, threadHelper, dataSetup => dataSetup.StartupExtantLanguageProject(projectId, true, progressDlg)))
			{
				if (progressDlg != null)
				{
					progressDlg.Step(0);
					// Reconstitute basics only.
					progressDlg.Step(0);
				}

				cache.ActionHandlerAccessor.BeginNonUndoableTask();

				CreateAnalysisWritingSystem(cache, analWrtSys, true);
				if (progressDlg != null)
					progressDlg.Step(0);
				CreateVernacularWritingSystem(cache, vernWrtSys, true);

				if (progressDlg != null)
					progressDlg.Step(0);
				AssignVernacularWritingSystemToDefaultPhPhonemes(cache);
				if (progressDlg != null)
					progressDlg.Step(0);

				var additionalAnalysisWss = (parameters.Length > 5 && parameters[5] != null)
												? (HashSet<IWritingSystem>)parameters[5]
												: new HashSet<IWritingSystem>();
				foreach (var additionalWs in additionalAnalysisWss)
					CreateAnalysisWritingSystem(cache, additionalWs, false);
				var additionalVernWss = (parameters.Length > 6 && parameters[6] != null)
											? (HashSet<IWritingSystem>)parameters[6]
											: new HashSet<IWritingSystem>();
				foreach (var additionalWs in additionalVernWss)
					CreateVernacularWritingSystem(cache, additionalWs, false);

				// Create a reversal index for the original default analysis writing system. (LT-4480)
				var riRepo = cache.ServiceLocator.GetInstance<IReversalIndexRepository>();
				riRepo.FindOrCreateIndexForWs(cache.DefaultAnalWs);
				if (progressDlg != null)
					progressDlg.Step(0);

				if (progressDlg != null)
					progressDlg.Position = nMax;

				cache.ActionHandlerAccessor.EndNonUndoableTask();
				cache.ActionHandlerAccessor.Commit();

				var xlist = new XmlList();

				// Load the semantic domain list.
				// Enhance: allow user to choose among alternative semantic domain lists?
				string sFile = Path.Combine(DirectoryFinder.TemplateDirectory, "SemDom.xml");
				if (progressDlg != null)
				{
					progressDlg.Message = Properties.Resources.ksLoadingSemanticDomains;
					// approximate number of items plus number of items with links.
					progressDlg.Minimum = 0;
					progressDlg.Maximum = 2200;
					progressDlg.Position = 0;
				}
				xlist.ImportList(cache.LangProject, "SemanticDomainList", sFile, progressDlg);

				// Load the intitial Part of Speech list.
				sFile = Path.Combine(DirectoryFinder.TemplateDirectory, "POS.xml");
				if (progressDlg != null)
				{
					progressDlg.Message = Properties.Resources.ksLoadingPartsOfSpeech;
					progressDlg.Minimum = 0;
					progressDlg.Maximum = 5;
					progressDlg.Position = 0;
				}
				xlist.ImportList(cache.LangProject, "PartsOfSpeech", sFile, progressDlg);
				cache.ActionHandlerAccessor.Commit();

				// Make sure that the new project has all the writing systems actually used by the
				// default data.  See FWR-1774.
				AddMissingWritingSystems(Path.Combine(DirectoryFinder.TemplateDirectory, DirectoryFinder.GetXmlDataFileName("NewLangProj")),
					cache.ServiceLocator.WritingSystemManager);
				AddMissingWritingSystems(Path.Combine(DirectoryFinder.TemplateDirectory, "POS.xml"),
					cache.ServiceLocator.WritingSystemManager);
				cache.ServiceLocator.WritingSystemManager.Save();

				cache.ActionHandlerAccessor.BeginNonUndoableTask();
				InitializeAnthroList(cache.LangProject,
									cache.WritingSystemFactory.GetWsFromStr(analWrtSys != null ? analWrtSys.IcuLocale : userIcuLocale),
									(parameters.Length > 7) ? (string)parameters[7] : null);
				ImportLocalizedLists(cache, progressDlg);
				cache.ActionHandlerAccessor.EndNonUndoableTask();

				NonUndoableUnitOfWorkHelper.Do(cache.ServiceLocator.GetInstance<IActionHandler>(), () =>
				{
					//Make sure the new project has the default location set up for the External files.
					cache.LanguageProject.LinkedFilesRootDir = Path.Combine(cache.ProjectId.ProjectFolder,
						DirectoryFinder.ksLinkedFilesDir);

					// Set the Date Created and Date Modified values.  (See FWR-3189.)
					cache.LangProject.DateCreated = DateTime.Now;
					cache.LangProject.DateModified = cache.LangProject.DateCreated;
				});
				cache.ActionHandlerAccessor.Commit();

				// Rewrite all objects to make sure they all have all of the basic properties.
				if (progressDlg != null)
				{
					progressDlg.ProgressBarStyle = ProgressBarStyle.Marquee;
					progressDlg.Message = AppStrings.InitializeSavingMigratedDataProgressMessage;
					progressDlg.RunTask(cache.SaveAndForceNewestXmlForCmObjectWithoutUnitOfWork,
						cache.m_serviceLocator.ObjectRepository.AllInstances().ToList());
				}
				else
				{
					// in case progressDlg is null, still do the work
					cache.SaveAndForceNewestXmlForCmObjectWithoutUnitOfWork(null, cache.m_serviceLocator.ObjectRepository.AllInstances().ToList());
				}
			}
			return ClientServerServices.Current.Local.ConvertToDb4oBackendIfNeeded(progressDlg, dbFileName);
		}

		/// <summary>
		/// Load the selected list from the file at ocmFile; otherwise, initialise an empty list
		/// </summary>
		/// <param name="proj">The language project</param>
		/// <param name="defaultWs">default writing system</param>
		/// <param name="ocmFile">path to the file containing the list to load</param>
		private static void InitializeAnthroList(ILangProject proj, int defaultWs, string ocmFile)
		{
			// Load the selected list, or initialize properly for a User-defined (empty) list.

			if (String.IsNullOrEmpty(ocmFile))
			{
				proj.AnthroListOA.Name.set_String(defaultWs, Strings.ksAnthropologyCategories);
				proj.AnthroListOA.Abbreviation.set_String(defaultWs, Strings.ksAnth);
				proj.AnthroListOA.ItemClsid = CmAnthroItemTags.kClassId;
				proj.AnthroListOA.Depth = 127;
			}
			else
			{
				var xlist = new XmlList();
				xlist.ImportList(proj, @"AnthroList", ocmFile, null);
			}

			// create the corresponding overlays if the list is not empty.

			ICmOverlay over = null;
			foreach (ICmOverlay x in proj.OverlaysOC)
			{
				if (x.PossListRA == proj.AnthroListOA)
				{
					over = x;
					break;
				}
			}
			if (over != null)
			{
				foreach (var poss in proj.AnthroListOA.PossibilitiesOS)
				{
					over.PossItemsRC.Add(poss);
					AddSubPossibilitiesToOverlay(over, poss);
				}
			}
		}

		private static void AddSubPossibilitiesToOverlay(ICmOverlay over, ICmPossibility poss)
		{
			foreach (var sub in poss.SubPossibilitiesOS)
			{
				over.PossItemsRC.Add(sub);
				AddSubPossibilitiesToOverlay(over, sub);
			}
		}

		/// <summary>
		/// Check for any localized lists files using pattern Templates\LocalizedLists-*.xml.
		/// If found, load each one into the project.
		/// </summary>
		private static void ImportLocalizedLists(FdoCache cache, IThreadedProgress progress)
		{
			var filePrefix = XmlTranslatedLists.LocalizedListPrefix;
			var rgsAnthroFiles = new List<string>();
			var rgsXmlFiles = Directory.GetFiles(DirectoryFinder.TemplateDirectory, filePrefix + "*.zip", SearchOption.TopDirectoryOnly);
			string sFile;
			for (var i = 0; i < rgsXmlFiles.Length; ++i)
			{
				string fileName = Path.GetFileNameWithoutExtension(rgsXmlFiles[i]);
				string wsId = fileName.Substring(filePrefix.Length);
				if (!IsWritingSystemInProject(wsId, cache))
					continue;
				NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(cache.ActionHandlerAccessor,
					() => ImportTranslatedLists(progress, new object[] {
																		  rgsXmlFiles[i],
																		  cache
																	   }
												));
			}
		}

		private static bool IsWritingSystemInProject(string wsId, FdoCache cache)
		{
			foreach (var ws in cache.ServiceLocator.WritingSystems.AllWritingSystems)
			{
				if (ws.RFC5646.ToLowerInvariant() == wsId.ToLowerInvariant())
					return true;
			}
			return false;
		}

		/// <summary>
		/// Import a file contained translated strings for one or more lists, using the
		/// given progress dialog.
		/// </summary>
		public static object ImportTranslatedLists(IThreadedProgress dlg, object[] parameters)
		{
			Debug.Assert(parameters.Length == 2);
			Debug.Assert(parameters[0] is string);
			var filename = (string)parameters[0];
			var cache = (FdoCache) parameters[1];
			var xtrans = new XmlTranslatedLists();
			return xtrans.ImportTranslatedLists(filename, cache, dlg);
		}

		/// <summary>
		/// Rewrites the given collection of ICmObjects to get the new xml for each.
		/// Make sure you do a normal Save first, so that the UndoStack knows all changes are saved.
		/// </summary>
		/// <param name="progressDlg"></param>
		/// <param name="parameters"></param>
		/// <remarks>
		/// This method does not use a UOW, so think about not using it.
		/// </remarks>
		private object SaveAndForceNewestXmlForCmObjectWithoutUnitOfWork(IProgress progressDlg, params object[] parameters)
		{
			var dirtballs = new HashSet<ICmObjectOrSurrogate>(new ObjectSurrogateEquater());
			foreach (var dirtball in (List<ICmObject>)parameters[0])
			{
				dirtballs.Add((ICmObjectOrSurrogate)dirtball);
			}
			var bep = (IDataStorer)m_serviceLocator.DataSetup;
			bep.Commit(new HashSet<ICmObjectOrSurrogate>(new ObjectSurrogateEquater()),
					   dirtballs,
					   new HashSet<ICmObjectId>());
			bep.CompleteAllCommits();
			return null;
		}

		/// <summary>
		/// Scan the (XML) file for writing systems that are not already in the local store, and
		/// add them.  A very quick and dirty scan is made.
		/// </summary>
		private static void AddMissingWritingSystems(string fileName,
			IWritingSystemManager wsm)
		{
			var mapLocalWs = new Dictionary<string, IWritingSystem>();
			foreach (var wsT in wsm.LocalWritingSystems)
				mapLocalWs.Add(wsT.Id, wsT);
			using (var rdr = new StreamReader(fileName, Encoding.UTF8))
			{
			string sLine;
			while ((sLine = rdr.ReadLine()) != null)
			{
				var idx = sLine.IndexOf(" ws=\"");
				if (idx < 0)
					continue;
				idx += 5;
				var idxLim = sLine.IndexOf("\"", idx);
				if (idxLim < 0)
					continue;
				var sWs = sLine.Substring(idx, idxLim - idx);
				if (mapLocalWs.ContainsKey(sWs))
					continue;
				IWritingSystem wsNew;
				wsm.GetOrSet(sWs, out wsNew);
				mapLocalWs.Add(sWs, wsNew);
			}
			rdr.Close();
		}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Build the file name for the new project and copy the template file (NewLangProj.fwdata).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static string CreateNewDbFile(ref string projectName)
		{
			projectName = MiscUtils.FilterForFileName(projectName, MiscUtils.FilenameFilterStrength.kFilterBackup);
			if (ProjectInfo.GetProjectInfoByName(projectName) != null)
				throw new ArgumentException("The specified project already exists.", "projectName");
			string dbDirName = Path.Combine(DirectoryFinder.ProjectsDirectory, projectName);
			string dbFileName = Path.Combine(dbDirName, DirectoryFinder.GetXmlDataFileName(projectName));
			try
			{
				Directory.CreateDirectory(dbDirName);
				CreateProjectSubfolders(dbDirName);
				// Make a copy of the template database that will become the new database
				File.Copy(Path.Combine(DirectoryFinder.TemplateDirectory,
					DirectoryFinder.GetXmlDataFileName("NewLangProj")), dbFileName, false);
				File.SetAttributes(dbFileName, FileAttributes.Normal);

				// Change the LangProject Guid to a new one to make it unique between projects, so Lift Bridge won't get cross with FLEx.
				var doc = XDocument.Load(dbFileName);
				var lpElement = doc.Element("languageproject").Elements("rt")
					.Where(rtEl => rtEl.Attribute("class").Value == "LangProject")
					.First();
				var newLpGuid = Guid.NewGuid().ToString().ToLowerInvariant();
				var guidAttr = lpElement.Attribute("guid");
				var oldLpGuid = guidAttr.Value.ToLowerInvariant();
				guidAttr.Value = newLpGuid;

				// Change all of the LP's owned stuff, so their ownerguid attrs are updated.
				foreach (var ownedEl in doc.Element("languageproject").Elements("rt").Where(ownedRt => ownedRt.Attribute("ownerguid") != null && ownedRt.Attribute("ownerguid").Value.ToLowerInvariant() == oldLpGuid))
				{
					ownedEl.Attribute("ownerguid").Value = newLpGuid;
				}

				doc.Save(dbFileName);
			}
			catch (Exception e)
			{
				throw new ApplicationException(projectName, e);
			}
			return dbFileName;
		}

		/// <summary>
		/// Create the subfolders for a newly created project.
		/// </summary>
		/// <remarks>This is not private because migrating a project from FieldWorks 6.0 to 7.0+ also needs this function.</remarks>
		internal static void CreateProjectSubfolders(string dbDirName)
		{
			try
			{
				Directory.CreateDirectory(DirectoryFinder.GetBackupSettingsDir(dbDirName));
				Directory.CreateDirectory(DirectoryFinder.GetConfigSettingsDir(dbDirName));
				Directory.CreateDirectory(DirectoryFinder.GetSupportingFilesDir(dbDirName));
				Directory.CreateDirectory(DirectoryFinder.GetDefaultMediaDir(dbDirName));
				Directory.CreateDirectory(DirectoryFinder.GetDefaultPicturesDir(dbDirName));
				Directory.CreateDirectory(DirectoryFinder.GetDefaultOtherExternalFilesDir(dbDirName));
				Directory.CreateDirectory(Path.Combine(dbDirName, DirectoryFinder.ksSortSequenceTempDir));
				Directory.CreateDirectory(DirectoryFinder.GetWritingSystemDir(dbDirName));
			}
			catch (Exception e)
			{
				MessageBoxUtils.Show(String.Format(AppStrings.ksCreateNewProjectSubfoldersError,
					e.Message));
				throw new ApplicationException(e.Message, e);
			}

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set an analysis writing system in the database and create one if needed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static void CreateAnalysisWritingSystem(FdoCache cache, IWritingSystem analWrtSys, bool fDefault)
		{
			string wsId = (analWrtSys == null) ? "en" : analWrtSys.Id;
			IWritingSystem wsAnalysis;
			cache.ServiceLocator.WritingSystemManager.GetOrSet(wsId, out wsAnalysis);

			// Add the writing system to the list of Analysis writing systems and make it the
			// first one in the list of current Analysis writing systems.
			if (fDefault)
				cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem = wsAnalysis;
			else
				cache.ServiceLocator.WritingSystems.AddToCurrentAnalysisWritingSystems(wsAnalysis);

			// hardwire to "en" for now - future should be below: "UserWs"
			if ("en" != wsId)
			{
				// Add the "en" writing system to the list of Analysis writing systems and
				// append it to the the list of current Analysis writing systems.
				IWritingSystem wsEN;
				cache.ServiceLocator.WritingSystemManager.GetOrSet("en", out wsEN);
				if (!cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems.Contains(wsEN))
					cache.ServiceLocator.WritingSystems.AddToCurrentAnalysisWritingSystems(wsEN);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new Vernacular writing system based on the name given.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static void CreateVernacularWritingSystem(FdoCache cache, IWritingSystem vernWrtSys, bool fDefault)
		{
			string wsId = (vernWrtSys == null) ? "fr" : vernWrtSys.Id;
			IWritingSystem wsVern;
			cache.ServiceLocator.WritingSystemManager.GetOrSet(wsId, out wsVern);

			// Add the writing system to the list of Vernacular writing systems and make it the
			// first one in the list of current Vernacular writing systems.
			if (fDefault)
			{
				cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem = wsVern;
				cache.LanguageProject.HomographWs = wsVern.Id;
			}
			else
				cache.ServiceLocator.WritingSystems.AddToCurrentVernacularWritingSystems(wsVern);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Assign the Vernacular writing system to all default PhCodes and PhPhoneme Names.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static void AssignVernacularWritingSystemToDefaultPhPhonemes(FdoCache cache)
		{
			// For all PhCodes in the default phoneme set, change the writing system from "en" to icuLocale
			var phSet = cache.LanguageProject.PhonologicalDataOA.PhonemeSetsOS[0];
			int wsVern = cache.DefaultVernWs;
			ITsStrFactory tsf = cache.TsStrFactory;
			foreach (var phone in phSet.PhonemesOC)
			{
				foreach (var code in phone.CodesOS)
				{

					code.Representation.VernacularDefaultWritingSystem =
						tsf.MakeString(code.Representation.UserDefaultWritingSystem.Text, wsVern);
				}
				phone.Name.VernacularDefaultWritingSystem =
					tsf.MakeString(phone.Name.UserDefaultWritingSystem.Text, wsVern);
			}
			foreach (var mrkr in phSet.BoundaryMarkersOC)
			{
				foreach (var code in mrkr.CodesOS)
				{
					code.Representation.VernacularDefaultWritingSystem =
						tsf.MakeString(code.Representation.UserDefaultWritingSystem.Text, wsVern);
				}
				mrkr.Name.VernacularDefaultWritingSystem =
					tsf.MakeString(mrkr.Name.UserDefaultWritingSystem.Text, wsVern);
			}
		}
		#endregion

		#region Public interface

		#region Public Properties
		/// <summary>
		/// Gets the lock object that should be used for all locking of objects within FDO.
		/// </summary>
		internal static object SyncRoot
		{
			get { return m_syncRoot; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the thread helper used for invoking actions on the main UI thread.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ThreadHelper ThreadHelper
		{
			get { return m_threadHelper; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the information about the loaded project.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IProjectIdentifier ProjectId
		{
			get { return m_serviceLocator.GetInstance<IDataSetup>().ProjectId; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the number of remote clients that are currently connected to the Database.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int NumberOfRemoteClients
		{
			get
			{
				if (!(ServiceLocator.GetInstance<IDataStorer>() is IClientServerDataManager))
					return 0; // not a CS backend.

				var serverName = ProjectId.ServerName;
				var projectName = ProjectId.Name;

				return ClientServerServices.Current.ListConnectedClients(serverName, projectName).Length - (ProjectId.IsLocal ? 1 : 0);
			}
		}

		/// <summary>
		/// Get the only LangProject in the data.
		/// </summary>
		public ILangProject LanguageProject
		{
			get
			{
				CheckDisposed();
				return m_serviceLocator.GetInstance<ILangProjectRepository>().Singleton;
			}
		}

		/// <summary>
		/// Get the only LangProject in the data.
		/// </summary>
		/// <remarks>
		/// Remove this and use LanguageProject, after the project is done.
		/// </remarks>
		public ILangProject LangProject
		{
			get
			{
				CheckDisposed();

				return LanguageProject;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <remarks>
		/// Remove this and use DomainDataByFlid.MetaDataCache, after the project is done.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public IFwMetaDataCache MetaDataCacheAccessor
		{
			get
			{
				CheckDisposed();
				return DomainDataByFlid.MetaDataCache;
			}
		}

		/// <summary>
		/// Get the ISilDataAccess cache used by the Views code.
		/// </summary>
		/// <remarks>
		/// This ISilDataAccess is not really a cache,
		/// as it works with the stateful FDO objects to get/set the data.
		/// This is a Facade over the entire FDO domain layer,
		/// so the property name isn't all that good now.
		/// </remarks>
		public ISilDataAccess DomainDataByFlid
		{
			get
			{
				CheckDisposed();
				return m_serviceLocator.GetInstance<ISilDataAccessManaged>();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <remarks>
		/// Remove this and use DomainDataByFlid, after the project is done.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public ISilDataAccess MainCacheAccessor
		{
			get
			{
				CheckDisposed();
				return DomainDataByFlid;
			}
		}

		/// <summary>
		/// Get the language writing system factory associated with the database associated with
		/// the underlying object.
		///</summary>
		/// <returns>A ILgWritingSystemFactory </returns>
		[Browsable(false)]
		public ILgWritingSystemFactory WritingSystemFactory
		{
			get
			{
				CheckDisposed();
				return m_lgwsFactory;
			}
		}

		/// <summary>
		///
		/// </summary>
		[Browsable(false)]
		public ILgWritingSystemFactory LanguageWritingSystemFactoryAccessor
		{
			get { return WritingSystemFactory; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This property provides the current ActionHandler.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IActionHandler ActionHandlerAccessor
		{
			get
			{
				CheckDisposed();

				return ServiceLocator.ActionHandler;
			}
		}

		/// <summary>
		/// Get a singleton ITsStrFactory for FDO.
		/// </summary>
		[Browsable(false)]
		public ITsStrFactory TsStrFactory
		{
			get
			{
				CheckDisposed();
				if (m_serviceLocator == null)
					return null;
				return m_serviceLocator.GetInstance<ITsStrFactory>();
			}
		}

		private int m_wsDefaultAnalysis;
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The default analysis writing system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int DefaultAnalWs
		{
			get
			{
				if (m_wsDefaultAnalysis == 0)
				{
					CheckDisposed();
					IWritingSystem ws = m_serviceLocator.WritingSystems.DefaultAnalysisWritingSystem;
					m_wsDefaultAnalysis = (ws == null ? 0 : ws.Handle);
				}
				return m_wsDefaultAnalysis;
			}
		}

		private int m_wsDefaultPron;
		/// <summary>
		/// The default pronunciation writing system.
		/// </summary>
		public int DefaultPronunciationWs
		{
			get
			{
				if (m_wsDefaultPron == 0)
				{
					CheckDisposed();
					IWritingSystem ws = m_serviceLocator.WritingSystems.DefaultPronunciationWritingSystem;
					m_wsDefaultPron = (ws == null ? 0 : ws.Handle);
				}
				return m_wsDefaultPron;
			}
		}

		private int m_wsDefaultVern;
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The default vernacular writing system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int DefaultVernWs
		{
			get
			{
				if (m_wsDefaultVern == 0)
				{
					CheckDisposed();
					IWritingSystem ws = m_serviceLocator.WritingSystems.DefaultVernacularWritingSystem;
					m_wsDefaultVern = (ws == null ? 0 : ws.Handle);
				}
				return m_wsDefaultVern;
			}
		}

		/// <summary>
		/// This should be called when anything changes that may make it necessary for
		/// DefaultVernWs or DefaultAnalWs to return a different result.
		/// </summary>
		internal void ResetDefaultWritingSystems()
		{
			m_wsDefaultVern = 0;
			m_wsDefaultAnalysis = 0;
			m_wsDefaultPron = 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the default (and only) UI writing system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int DefaultUserWs
		{
			get
			{
				CheckDisposed();
				return WritingSystemFactory.UserWs;
			}
		}

		#endregion Public Properties

		#region Public methods
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Change the name of the database represented by this connection.
		/// </summary>
		/// <param name="sNewName"></param>
		/// <returns>true if successful, false if rename failed</returns>
		/// -----------------------------------------------------------------------------------
		public bool RenameDatabase(string sNewName)
		{
			CheckDisposed();

			if (m_serviceLocator.GetInstance<IDataSetup>().RenameDatabase(sNewName))
			{
				if (ProjectNameChanged != null)
					ProjectNameChanged(this);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Get the ServiceLocator for this system/database.
		/// </summary>
		/// <returns></returns>
		public IFdoServiceLocator ServiceLocator
		{
			get { return m_serviceLocator; }
		}

		/// <summary>
		/// Add to the list a ClassAndPropInfo for each concrete class of object that may be added to
		/// property flid.
		/// (Note that at present this does not depend on the object. But we expect eventually
		/// that this will become a method of FDO.CmObject.
		/// </summary>
		/// <param name="flid"></param>
		/// <param name="excludeAbstractClasses"></param>
		/// <param name="list"></param>
		public void AddClassesForField(int flid, bool excludeAbstractClasses, List<ClassAndPropInfo> list)
		{
			CheckDisposed();
			var mdc = (IFwMetaDataCacheManaged) DomainDataByFlid.MetaDataCache;
			if (mdc.GetFieldNameOrNull(flid) == null)
				return; // a decorator field.
			var clsidDst = GetDestinationClass(flid);
			if (clsidDst == 0)
				return;	// not enough information from this flid.

			var fieldName = mdc.GetFieldName(flid);
			var fldType = mdc.GetFieldType(flid);
			foreach (var clsidPossDst in mdc.GetAllSubclasses(clsidDst))
			{
				var cpi = new ClassAndPropInfo
							{
								fieldName = fieldName,
								flid = flid,
								signatureClsid = clsidPossDst,
								fieldType = fldType,
								signatureClassName = mdc.GetClassName(clsidPossDst),
								isAbstract = mdc.GetAbstract(clsidPossDst),
								isBasic = fldType < (int) CellarPropertyType.MinObj,
								isCustom = GetIsCustomField(flid),
								isReference = IsReferenceProperty(flid),
								isVector = IsVectorProperty(flid)
							};
				list.Add(cpi);
				if (excludeAbstractClasses && cpi.isAbstract)
					list.Remove(cpi);
			}
		}

		/// <summary>
		/// Gets the destination class of the given flid, (try even from a virtual handler).
		/// </summary>
		/// <param name="flid"></param>
		/// <returns></returns>
		public int GetDestinationClass(int flid)
		{
			CheckDisposed();
			return DomainDataByFlid.MetaDataCache.GetDstClsId(flid);
		}

		/// <summary>
		/// determine if the flid is in the custom range
		/// </summary>
		/// <param name="flid"></param>
		/// <returns></returns>
		public bool GetIsCustomField(int flid)
		{
			CheckDisposed();
			long remainder;
			Math.DivRem(flid, 1000, out remainder);
			return remainder >= 500;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Returns an integer giving the number of senses in a given LexEntry.</summary>
		/// <param name='obj'>The object for which we want a number of senses (an ILexEntry).</param>
		/// <param name="flid">The Senses field.</param>
		/// <param name='fIncSubSenses'>True if you want to include nested senses in the count.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public int GetNumberOfSensesForEntry(ICmObject obj, int flid, bool fIncSubSenses)
		{
			CheckDisposed();

			if (obj == null)
				throw new ArgumentNullException("obj");

			var hvoTarget = obj.Hvo;
			int cSenses = DomainDataByFlid.get_VecSize(hvoTarget, flid);
			if (cSenses == 0 || !fIncSubSenses)
				return cSenses;
			int loopMax = cSenses;
			// Next iteration: treat each sense from this cycle as the target object for the next.
			for (int i = 0; i < loopMax; i++)
			{
				cSenses += GetNumberOfSubSenses(DomainDataByFlid.get_VecItem(hvoTarget, flid, i));
			}
			return cSenses;
		}

		private int GetNumberOfSubSenses(int hvoSense)
		{
			int cSubSenses = DomainDataByFlid.get_VecSize(hvoSense, LexSenseTags.kflidSenses);
			if (cSubSenses == 0)
				return 0;
			int loopMax = cSubSenses;
			// Next iteration: treat each sense from this cycle as the target object for the next.
			for (int i = 0; i < loopMax; i++)
			{
				cSubSenses += GetNumberOfSubSenses(DomainDataByFlid.get_VecItem(hvoSense, LexSenseTags.kflidSenses, i));
			}
			return cSubSenses;
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>Return a string giving an outline number such as 1.2.3 based on position in
		/// the owning hierarcy.
		/// </summary>
		/// <param name='obj'>The object for which we want an outline number.</param>
		/// <param name="flid">The hierarchical field.</param>
		/// <param name='fFinalPeriod'>True if you want a final period appended to the string.</param>
		/// <param name='fIncTopOwner'>True if you want to include the index number of the owner
		/// that does not come from the same field, but from a similar one (same type and
		/// destination class).</param>
		/// <returns>A System.String</returns>
		/// ------------------------------------------------------------------------------------
		public string GetOutlineNumber(ICmObject obj, int flid, bool fFinalPeriod, bool fIncTopOwner)
		{
			return GetOutlineNumber(obj, flid, fFinalPeriod, fIncTopOwner, DomainDataByFlid);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Return a string giving an outline number such as 1.2.3 based on position in
		/// the owning hierarcy.
		/// </summary>
		/// <param name='obj'>The object for which we want an outline number.</param>
		/// <param name="flid">The hierarchical field.</param>
		/// <param name='fFinalPeriod'>True if you want a final period appended to the string.</param>
		/// <param name='fIncTopOwner'>True if you want to include the index number of the owner
		/// that does not come from the same field, but from a similar one (same type and
		/// destination class).</param>
		/// <param name="sda">SDA in which to look up object position.</param>
		/// <returns>A System.String</returns>
		/// ------------------------------------------------------------------------------------
		public string GetOutlineNumber(ICmObject obj, int flid, bool fFinalPeriod, bool fIncTopOwner, ISilDataAccess sda)
		{
			CheckDisposed();

			if (obj == null)
				throw new ArgumentNullException("obj");

			if (!fIncTopOwner)
				throw new NotImplementedException("GetOutlineNumber not including top owner is not implemented yet.");

			string sNum = "";
			var target = obj;
			while (true)
			{
				var owner = target.Owner;
				var mdc = MetaDataCache;
				int ownerFlid = target.OwningFlid;
				// If we have no ownerflid this is an unowned object (probably a LexEntry) and we
				// can't go any further. Likewise if it is owned in a collection there is no
				// meaningful position so we have to stop.
				if (ownerFlid == 0 || mdc.GetFieldType(ownerFlid) != (int)CellarPropertyType.OwningSequence)
					break;
				var ihvo = sda.GetObjIndex(owner.Hvo, ownerFlid, target.Hvo);

				// ihvo has to be >= 0 at this point, since we searched for the object in its own owning property.
				sNum = sNum.Length == 0 ? string.Format("{0}", ihvo + 1) : string.Format("{0}.{1}", ihvo + 1, sNum);
				if (ownerFlid != flid)
					break; // process only one level of owner that isn't the expected hierarchical property.
				// Next iteration: treat the owner from this cycle as the target object for the next.
				target = owner;
			}
			if (fFinalPeriod)
				sNum += ".";
			return sNum;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Return a string giving an outline number such as 1.2.3 based on position in
		/// the owning hierarcy.
		/// </summary>
		/// <param name='obj'>The object for which we want an outline number.</param>
		/// <param name='fFinPer'>True if you want a final period appended to the string.</param>
		/// <param name='fIncTopOwner'>True if you want to include the index number of the owner
		/// that does not come from the same field, but from a similar one (same type and
		/// destination class).</param>
		/// <returns>A System.String</returns>
		/// ------------------------------------------------------------------------------------
		public string GetOutlineNumber(ICmObject obj, bool fFinPer, bool fIncTopOwner)
		{
			return GetOutlineNumber(obj, obj.OwningFlid, fFinPer, fIncTopOwner);
		}

		/// <summary>
		/// This variant of GetObject is mainly useful for creating an object out of the integer value of an atomic property.
		/// If the argument is the value we reserve to indicate an empty atomic property, return null, otherwise, the appropriate object.
		/// NB: it does not return null for EVERY invalid HVO, only kNullHvo. Other invalid HVOs will still throw exceptions.
		/// </summary>
		/// <param name="hvo"></param>
		/// <returns></returns>
		public ICmObject GetAtomicPropObject(int hvo)
		{
			CheckDisposed();

			return hvo == kNullHvo ? null : m_serviceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Checks a field id to see if it is a collection or sequence property.</summary>
		/// <param name="flid">Field ID to be checked.</param>
		/// <returns>
		/// true, if flid is a collection or sequence property, otherwise false.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public bool IsVectorProperty(int flid)
		{
			CheckDisposed();

			var iType = (CellarPropertyType)DomainDataByFlid.MetaDataCache.GetFieldType(flid);
			return ((iType == CellarPropertyType.OwningCollection)
				|| (iType == CellarPropertyType.OwningSequence)
				|| (iType == CellarPropertyType.ReferenceCollection)
				|| (iType == CellarPropertyType.ReferenceSequence));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>Checks a field ID to see if it is a reference property.</summary>
		/// <param name="flid">Field ID to be checked.</param>
		/// <returns>
		/// true, if flid is a reference property, otherwise false.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public bool IsReferenceProperty(int flid)
		{
			CheckDisposed();

			var iType = (CellarPropertyType)DomainDataByFlid.MetaDataCache.GetFieldType(flid);
			return ((iType == CellarPropertyType.ReferenceAtomic)
				|| (iType == CellarPropertyType.ReferenceCollection)
				|| (iType == CellarPropertyType.ReferenceSequence));
		}

		/// <summary>
		/// Answer whether clidTest is, or is a subclass of, clidSig.
		/// That is, either clidTest is the same as clidSig, or one of the base classes of clidTest is clidSig.
		/// As a special case, if clidSig is 0, all classes are considered to match
		/// </summary>
		/// <param name="clidTest"></param>
		/// <param name="clidSig"></param>
		/// <returns></returns>
		public bool ClassIsOrInheritsFrom(int clidTest, int clidSig)
		{
			CheckDisposed();

			if (clidSig == 0)
				return true; // Everything derives from CmObject.

			var mdc = DomainDataByFlid.MetaDataCache;
			for (var clidBase = clidTest; clidBase != 0; clidBase = mdc.GetBaseClsId(clidTest))
			{
				if (clidBase == clidSig)
					return true;

				clidTest = clidBase;
			}

			return false;
		}

		/// <summary>
		/// Returns the string value for the given flid. If the flid does not refer to a supported
		/// string type, fTypeFound is set to false.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="flid"></param>
		/// <param name="frag">XmlNode containing the ws id for Multi string types.</param>
		/// <param name="fTypeFound">true if the flid refers to a supported string type, false otherwise.</param>
		/// <returns>if fTypeFound is true we return the string value for the flid. Otherwise we return </returns>
		public string GetText(int hvo, int flid, XmlNode frag, out bool fTypeFound)
		{
			CheckDisposed();

			var itype = (CellarPropertyType)DomainDataByFlid.MetaDataCache.GetFieldType(flid);
			fTypeFound = true;

			switch (itype)
			{
				case CellarPropertyType.Unicode:
					return DomainDataByFlid.get_UnicodeProp(hvo, flid);
				case CellarPropertyType.String:
					return DomainDataByFlid.get_StringProp(hvo, flid).Text;
				case CellarPropertyType.MultiString:
				case CellarPropertyType.MultiUnicode:
					{
						var wsid = WritingSystemServices.GetWritingSystem(this, frag, null, hvo, flid, 0).Handle;
						return wsid == 0 ? string.Empty : DomainDataByFlid.get_MultiStringAlt(hvo, flid, wsid).Text;
					}
				default:
					// This string type is not supported.
					fTypeFound = false;
					return itype.ToString();
			}
		}

		/// <summary>
		/// Ensure a valid folder for LangProject.LinkedFilesRootDir.  When moving projects
		/// between systems, the stored value may become hopelessly invalid.  See FWNX-1005
		/// for an example of the havoc than can ensue.
		/// </summary>
		/// <remarks>This method gets called when we open the FDO cache.</remarks>
		private void EnsureValidLinkedFilesFolder()
		{
			if (MiscUtils.RunningTests)
				return;

			var linkedFilesFolder = this.LangProject.LinkedFilesRootDir;
			var defaultFolder = DirectoryFinder.GetDefaultLinkedFilesDir(this.ProjectId.ProjectFolder);
			EnsureValidLinkedFilesFolderCore(linkedFilesFolder, defaultFolder);

			if (!Directory.Exists(linkedFilesFolder))
			{
				if (!Directory.Exists(defaultFolder))
					defaultFolder = this.ProjectId.ProjectFolder;
				MessageBox.Show(String.Format(Strings.ksInvalidLinkedFilesFolder, linkedFilesFolder), Strings.ksErrorCaption);
				while (!Directory.Exists(linkedFilesFolder))
				{
					using (var folderBrowserDlg = new FolderBrowserDialogAdapter())
					{
						folderBrowserDlg.Description = Strings.ksLinkedFilesFolder;
						folderBrowserDlg.RootFolder = Environment.SpecialFolder.Desktop;
						folderBrowserDlg.SelectedPath = defaultFolder;
						if (folderBrowserDlg.ShowDialog() == DialogResult.OK)
							linkedFilesFolder = folderBrowserDlg.SelectedPath;
					}
				}
				NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(this.ActionHandlerAccessor, () =>
					{ this.LangProject.LinkedFilesRootDir = linkedFilesFolder; });
			}
		}

		/// <summary>
		/// Just make the directory if it's the default.
		/// See FWNX-1092, LT-14491.
		/// </summary>
		internal void EnsureValidLinkedFilesFolderCore(string linkedFilesFolder, string defaultLinkedFilesFolder)
		{
			if (linkedFilesFolder == defaultLinkedFilesFolder)
				FileUtils.EnsureDirectoryExists(defaultLinkedFilesFolder);
		}
		#endregion Public methods

		#endregion Public interface

		#region Internal interface

		#region Internal Properties

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Indicates whether this cache is fully initialized (needed because some objects' data
		/// consistency checking must not be done until this is true). This should never get set
		/// to false after being true.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal bool FullyInitializedAndReadyToRock { get; private set; }

		/// <summary>
		/// Get the MetaDataCache.
		/// </summary>
		internal IFwMetaDataCache MetaDataCache
		{
			get { return m_serviceLocator.GetInstance<IFwMetaDataCacheManaged>(); }
		}

		/// <summary>
		/// An object is inserted into this at the beginning of CmObjectIternal.DeleteObject, and removed at the end.
		/// It is used to suppress certain side effects; in particular, CmObject.CanDelete returns false if the object
		/// is already in the process of being deleted.
		/// </summary>
		internal HashSet<ICmObject> ObjectsBeingDeleted { get { return m_objectsBeingDeleted; } }

		#endregion Internal Properties

		#region Internal Methods

		/// <summary>
		/// Constructor.
		/// </summary>
		internal FdoCache()
		{}

		internal ITsString MakeUserTss(string val)
		{
			return TsStrFactory.MakeString(val, WritingSystemFactory.UserWs);
		}

		#endregion Internal Methods

		#endregion Internal interface

		#region Private interface

		#region Private Properties

		#endregion Private Properties

		#endregion Private interface

		/// <summary>
		/// Return a string (typically at or near shutdown) which may be passed back to NewObjectsSinceVersion.
		/// </summary>
		public string VersionStamp
		{
			get
			{
				var csdm = ServiceLocator.GetInstance<IDataStorer>() as IClientServerDataManager;
				if (csdm == null)
					return null;
				return csdm.VersionStamp;
			}
		}

		/// <summary>
		/// Pass as versionStamp a string previously obtained from VersionStamp. Answer true if changes saved to
		/// the database since versionStamp included the creation of new objects of class classname.
		/// If the backend cannot be sure, it should answer true; this method is used to suppress use of locally
		/// persisted lists as an optimization.
		/// </summary>
		public bool NewObjectsSinceVersion(string versionStamp, string classname)
		{
			var csdm = ServiceLocator.GetInstance<IDataStorer>() as IClientServerDataManager;
			if (csdm == null)
				return false; // not a CS backend, there should be no way for it to get out of date.
			return csdm.NewObjectsSinceVersion(versionStamp, classname);
		}
	}

	#region Cache Pair, one attached to database and one purely in memory
	/// <summary>
	/// CachePair maintains a relationship between two caches, a regular FdoCache storing real data,
	/// and an ISilDataAccess, typically a VwCacheDaClass, that stores temporary data for a
	/// secondary view. As well as storing both cache objects, it stores two maps which maintain a
	/// bidirectional link between HVOs in one and those in the other.
	/// </summary>
	public class CachePair : IFWDisposable
	{
		private FdoCache m_fdoCache;
		private ISilDataAccess m_sda;
		private Dictionary<int, int> m_FdoToSda;
		private Dictionary<int, int> m_SdaToFdo;
		private ICmObjectRepository m_coRepository;

		#region IDisposable & Co. implementation
		// Region last reviewed: never

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// True, if the object has been disposed.
		/// </summary>
		private bool m_isDisposed;

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~CachePair()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <summary>
		///
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected virtual void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_FdoToSda != null)
					m_FdoToSda.Clear();
				if (m_SdaToFdo != null)
					m_SdaToFdo.Clear();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			Marshal.ReleaseComObject(m_sda);
			m_sda = null;
			m_fdoCache = null;
			m_FdoToSda = null;
			m_SdaToFdo = null;
			m_coRepository = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		/// <summary>
		/// Forget any previously-established relationships between objects in the main
		/// and secondary caches.
		/// </summary>
		public void ClearMaps()
		{
			CheckDisposed();
			m_SdaToFdo.Clear();
			m_FdoToSda.Clear();
		}
		/// <summary>
		///
		/// </summary>
		public FdoCache MainCache
		{
			get
			{
				CheckDisposed();
				return m_fdoCache;
			}
			set
			{
				CheckDisposed();
				if (m_fdoCache == value)
					return;

				m_fdoCache = value;
				// Forget any existing relationships.
				m_FdoToSda = new Dictionary<int, int>();
				m_SdaToFdo = new Dictionary<int, int>();
				m_coRepository = m_fdoCache.ServiceLocator.GetInstance<ICmObjectRepository>();
			}
		}

		/// <summary>
		///
		/// </summary>
		public ISilDataAccess DataAccess
		{
			get
			{
				CheckDisposed();
				return m_sda;
			}
			set
			{
				CheckDisposed();
				if (m_sda == value)
					return;
				if (m_sda != null)
					Marshal.ReleaseComObject(m_sda);
				m_sda = value;
				// Forget any existing relationships.
				m_FdoToSda = new Dictionary<int, int>();
				m_SdaToFdo = new Dictionary<int, int>();
			}
		}

		/// <summary>
		/// Create a new secondary cache.
		/// </summary>
		public void CreateSecCache()
		{
			CheckDisposed();
			DataAccess = VwCacheDaClass.Create();
			DataAccess.WritingSystemFactory = m_fdoCache.WritingSystemFactory;
		}

		/// <summary>
		/// Map from secondary hvo (in the SilDataAccess) to real hvo (in the FdoCache).
		/// </summary>
		/// <param name="secHvo"></param>
		/// <returns></returns>
		public int RealHvo(int secHvo)
		{
			CheckDisposed();
			if (m_SdaToFdo.ContainsKey(secHvo))
				return m_SdaToFdo[secHvo];

			return 0;
		}

		/// <summary>
		/// Map from secondary hvo (in the SilDataAccess) to real object (in ICmOjectRepository).
		/// </summary>
		/// <param name="secHvo"></param>
		/// <returns></returns>
		public ICmObject RealObject(int secHvo)
		{
			int hvoReal = RealHvo(secHvo);
			if (hvoReal != 0 && m_coRepository != null)
				return m_coRepository.GetObject(hvoReal);
			return null;
		}

		/// <summary>
		/// Create a two-way mapping.
		/// </summary>
		/// <param name="secHvo">SilDataAccess HVO</param>
		/// <param name="realHvo">In the FDO Cache</param>
		public void Map(int secHvo, int realHvo)
		{
			CheckDisposed();
			m_SdaToFdo[secHvo] = realHvo;
			m_FdoToSda[realHvo] = secHvo;
		}

		/// <summary>
		/// Removes a two-way mapping.
		/// </summary>
		/// <param name="secHvo">SilDataAccess HVO</param>
		/// <returns><c>true</c> if the mapping was successfully removed, otherwise <c>false</c>.</returns>
		public bool RemoveSec(int secHvo)
		{
			CheckDisposed();
			int realHvo;
			if (m_SdaToFdo.TryGetValue(secHvo, out realHvo))
				m_FdoToSda.Remove(realHvo);
			return m_SdaToFdo.Remove(secHvo);
		}

		/// <summary>
		/// Removes a two-way mapping.
		/// </summary>
		/// <param name="realHvo">In the FDO Cache</param>
		/// <returns><c>true</c> if the mapping was successfully removed, otherwise <c>false</c>.</returns>
		public bool RemoveReal(int realHvo)
		{
			CheckDisposed();
			int secHvo;
			if (m_FdoToSda.TryGetValue(realHvo, out secHvo))
				m_SdaToFdo.Remove(secHvo);
			return m_FdoToSda.Remove(realHvo);
		}

		/// <summary>
		/// Map from real hvo (in the FdoCache) to secondary (in the SilDataAccess).
		/// </summary>
		/// <param name="realHvo"></param>
		/// <returns></returns>
		public int SecHvo(int realHvo)
		{
			CheckDisposed();
			if (m_FdoToSda.ContainsKey(realHvo))
				return m_FdoToSda[realHvo];

			return 0;
		}

		/// <summary>
		/// Look for a secondary-cache object that corresponds to hvoReal. If one does not already exist,
		/// create it by appending to property flidOwn of object hvoOwner.
		/// </summary>
		/// <param name="hvoReal"></param>
		/// <param name="clid"></param>
		/// <param name="hvoOwner"></param>
		/// <param name="flidOwn"></param>
		/// <returns></returns>
		public int FindOrCreateSec(int hvoReal, int clid, int hvoOwner, int flidOwn)
		{
			CheckDisposed();
			int hvoSec = 0;
			if (hvoReal != 0)
				hvoSec = SecHvo(hvoReal);
			if (hvoSec == 0)
			{
				hvoSec = m_sda.MakeNewObject(clid, hvoOwner, flidOwn, m_sda.get_VecSize(hvoOwner, flidOwn));
				if (hvoReal != 0)
					Map(hvoSec, hvoReal);
			}
			return hvoSec;
		}
		/// <summary>
		/// Look for a secondary-cache object that corresponds to hvoReal. If one does not already exist,
		/// create it by appending to property flidOwn of object hvoOwner.
		/// Set its flidName property to a string name in writing system ws.
		/// If hvoReal is zero, just create an object, but don't look for or create an association.
		/// </summary>
		/// <param name="hvoReal"></param>
		/// <param name="clid"></param>
		/// <param name="hvoOwner"></param>
		/// <param name="flidOwn"></param>
		/// <param name="flidName"></param>
		/// <param name="tss"></param>
		/// <returns></returns>
		public int FindOrCreateSec(int hvoReal, int clid, int hvoOwner, int flidOwn, int flidName, ITsString tss)
		{
			CheckDisposed();
			int hvoSec = FindOrCreateSec(hvoReal, clid, hvoOwner, flidOwn);
			m_sda.SetString(hvoSec, flidName, tss);
			return hvoSec;
		}

		/// <summary>
		/// Look for a secondary-cache object that corresponds to hvoReal. If one does not already exist,
		/// create it by appending to property flidOwn of object hvoOwner.
		/// Set its flidName property to a string name in writing system ws.
		/// </summary>
		/// <param name="hvoReal"></param>
		/// <param name="clid"></param>
		/// <param name="hvoOwner"></param>
		/// <param name="flidOwn"></param>
		/// <param name="name"></param>
		/// <param name="flidName"></param>
		/// <param name="ws"></param>
		/// <returns></returns>
		public int FindOrCreateSec(int hvoReal, int clid, int hvoOwner, int flidOwn, string name, int flidName, int ws)
		{
			CheckDisposed();

			return FindOrCreateSec(hvoReal, clid, hvoOwner, flidOwn, flidName, m_fdoCache.TsStrFactory.MakeString(name, ws));
		}
		/// <summary>
		/// Like FindOrCreateSec, except the ws is taken automaticaly as the default analysis ws of the main cache.
		/// </summary>
		/// <param name="hvoReal"></param>
		/// <param name="clid"></param>
		/// <param name="hvoOwner"></param>
		/// <param name="flidOwn"></param>
		/// <param name="name"></param>
		/// <param name="flidName"></param>
		/// <returns></returns>
		public int FindOrCreateSecAnalysis(int hvoReal, int clid, int hvoOwner, int flidOwn, string name, int flidName)
		{
			CheckDisposed();
			return FindOrCreateSec(hvoReal, clid, hvoOwner, flidOwn, name,
				flidName,
				m_fdoCache.DefaultAnalWs);
		}

		/// <summary>
		/// Like FindOrCreateSec, except the ws is taken automaticaly as the default vernacular ws of the main cache.
		/// </summary>
		/// <param name="hvoReal"></param>
		/// <param name="clid"></param>
		/// <param name="hvoOwner"></param>
		/// <param name="flidOwn"></param>
		/// <param name="name"></param>
		/// <param name="flidName"></param>
		/// <returns></returns>
		public int FindOrCreateSecVern(int hvoReal, int clid, int hvoOwner, int flidOwn, string name, int flidName)
		{
			CheckDisposed();
			return FindOrCreateSec(hvoReal, clid, hvoOwner, flidOwn, name,
				flidName,
				m_fdoCache.DefaultVernWs);
		}
	}


	#endregion Cache Pair, one attached to database and one purely in memory

	/// <summary>
	/// Holds information about one property of an object and one type of object we could create there.
	/// </summary>
	/// <remarks>
	/// The ClassFieldInfo struct and the ClassAndPropInfo class need to be combined.
	/// </remarks>
	public class ClassAndPropInfo
	{
		/// <summary></summary>
		public const int kposNotSet = -3; // must be negative. Good to avoid -1 and -2 which have other special values for some functions.
		/// <summary></summary>
		public int flid; // an owning property in which we could create an object.
		/// <summary></summary>
		public string fieldName; // the text name of that property.
		/// <summary>the class that has the property</summary>
		public int sourceClsid;
		/// <summary></summary>
		public int signatureClsid; // a class of object we could add to that property
		/// <summary></summary>
		public string signatureClassName; // the text name of that class.
		/// <summary></summary>
		public int fieldType; // CmTypes constant kcptOwningAtom, kcptOwningCollection, kcptOwningSequence.
		// These two may be left out if the client knows where to create.
		/// <summary></summary>
		public int hvoOwner; // Thing that will own the newly created object.
		/// <summary></summary>
		public int ihvoPosition = kposNotSet; // Place it will occupy in the property; or not set.
		// For a collection, this is the currently cached position.
		/// <summary></summary>
		public bool isAbstract;
		/// <summary></summary>
		public bool isVector;
		/// <summary></summary>
		public bool isReference;
		/// <summary></summary>
		public bool isBasic;
		/// <summary>is this a custom user field?</summary>
		public bool isCustom;
	}

	/// <summary>
	///
	/// </summary>
	public abstract class FdoVectorUtils
	{
		/// <summary>
		///
		/// </summary>
		/// <typeparam name="TDerived"></typeparam>
		/// <param name="cmObjects"></param>
		/// <returns></returns>
		public static List<int> ConvertCmObjectsToHvoList<TDerived>(IEnumerable<TDerived> cmObjects)
		   where TDerived : ICmObject
		{
			return new List<int>(ConvertCmObjectsToHvos(cmObjects));
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="cmObjects"></param>
		/// <returns></returns>
		public static IEnumerable<int> ConvertCmObjectsToHvos<TDerived>(IEnumerable<TDerived> cmObjects)
			where TDerived : ICmObject
		{
			foreach (ICmObject cmObject in cmObjects)
				yield return cmObject.Hvo;
		}
	}
}
