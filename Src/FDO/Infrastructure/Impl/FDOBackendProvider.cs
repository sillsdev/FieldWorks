// --------------------------------------------------------------------------------------------
// Copyright (C) 2009 SIL International. All rights reserved.
//
// Distributable under the terms of either the Common Public License or the
// GNU Lesser General Public License, as specified in the LICENSING.txt file.
//
// File: FDOBackendProvider.cs
// Responsibility: John Thomson, Steve Miller
// Last reviewed: never
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.DomainServices.DataMigration;

namespace SIL.FieldWorks.FDO.Infrastructure.Impl
{
	/// <summary>
	/// A common base class for all FDO backend providers.
	/// </summary>
	internal abstract partial class FDOBackendProvider : IDataSetup, IDataReader, IDataStorer
	{
		// Assume pure lazy loading ('on demand').
		// The Initialize method will then give the right answer,
		// and the client can load more domains, as needed.
		private LoadedDomains m_loadedDomains = new LoadedDomains(false, false, false, false);
		protected readonly IdentityMap m_identityMap;
		protected readonly ICmObjectSurrogateFactory m_surrogateFactory;
		protected readonly FdoCache m_cache;
		protected readonly IFwMetaDataCacheManagedInternal m_mdcInternal;
		private readonly IDataMigrationManager m_dataMigrationManager;
		protected readonly Dictionary<string, CustomFieldInfo> m_extantCustomFields = new Dictionary<string, CustomFieldInfo>();
		protected int m_modelVersionOverride = ModelVersion;
		private readonly List<Thread> m_loadDomainThreads = new List<Thread>();
		private readonly object m_syncRoot = new object();
		protected volatile bool m_stopLoadDomain;

		/// <summary>
		///
		/// </summary>
		protected FDOBackendProvider(FdoCache cache, IdentityMap identityMap,
			ICmObjectSurrogateFactory surrogateFactory, IFwMetaDataCacheManagedInternal mdc, IDataMigrationManager dataMigrationManager)
		{
			if (cache == null) throw new ArgumentNullException("cache");
			if (identityMap == null) throw new ArgumentNullException("identityMap");
			if (surrogateFactory == null) throw new ArgumentNullException("surrogateFactory");
			if (dataMigrationManager == null) throw new ArgumentNullException("dataMigrationManager");

			m_cache = cache;
			m_cache.Disposing += OnCacheDisposing;
			m_identityMap = identityMap;
			m_surrogateFactory = surrogateFactory;
			m_mdcInternal = mdc;
			m_dataMigrationManager = dataMigrationManager;
		}


		#region IFWDisposable implementation

		/// <summary>
		/// True, if the object has been disposed.
		/// </summary>
		private bool m_isDisposed;

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException("'FDOBackendProvider' in use after being disposed.");
		}

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
		~FDOBackendProvider()
		{
			// If we come here it means that haven't called Dispose() where we should have.
			// Although GC automatically calls the finalizer and releases all resources,
			// relying on the GC when there is a Dispose() method is bad (GC runs on a different
			// thread; indeterministic order in which finalizers are called on the different
			// objects; we get sporadic failing or even hanging tests)
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
		/// it needs to be handled by fixing the problem.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected virtual void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + ". ****** ");
			// Can be called more than once,
			// but only do things the first time.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				ShutdownInternal();

				ShutdownRunningThreads();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.

			m_isDisposed = true;
		}

		#endregion // IFWDisposable implementation

		/// <summary>
		/// Cache is about to be disposed. We need to stop any running loadDomain thread.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnCacheDisposing(object sender, EventArgs e)
		{
			ShutdownRunningThreads();
		}

		/// <summary>
		/// Stop any thread that might still be running
		/// </summary>
		private void ShutdownRunningThreads()
		{
			m_stopLoadDomain = true;
			foreach (var thread in m_loadDomainThreads)
			{
				if (!thread.Join(1000))
				{
					thread.Abort();
					thread.Join(1000);
				}
			}
			m_loadDomainThreads.Clear();
		}

		protected static CellarPropertyType GetFlidTypeFromString(string type)
		{
			CellarPropertyType flidType;
			switch (type)
			{
				default:
					throw new ArgumentException("Property element name not recognized.");
				case "Boolean":
					flidType = CellarPropertyType.Boolean;
					break;
				case "Integer":
					flidType = CellarPropertyType.Integer;
					break;
				case "Time":
					flidType = CellarPropertyType.Time;
					break;
				case "String":
					flidType = CellarPropertyType.String;
					break;
				case "MultiString":
					flidType = CellarPropertyType.MultiString;
					break;
				case "Unicode":
					flidType = CellarPropertyType.Unicode;
					break;
				case "MultiUnicode":
					flidType = CellarPropertyType.MultiUnicode;
					break;
				case "Guid":
					flidType = CellarPropertyType.Guid;
					break;
				case "Image":
					flidType = CellarPropertyType.Image;
					break;
				case "GenDate":
					flidType = CellarPropertyType.GenDate;
					break;
				case "Binary":
					flidType = CellarPropertyType.Binary;
					break;
				case "Numeric":
					flidType = CellarPropertyType.Numeric;
					break;
				case "Float":
					flidType = CellarPropertyType.Float;
					break;
				case "OA":
				case "OwningAtom":
					flidType = CellarPropertyType.OwningAtomic;
					break;
				case "OC":
					flidType = CellarPropertyType.OwningCollection;
					break;
				case "OS":
					flidType = CellarPropertyType.OwningSequence;
					break;
				case "RA":
					flidType = CellarPropertyType.ReferenceAtomic;
					break;
				case "RC":
					flidType = CellarPropertyType.ReferenceCollection;
					break;
				case "RS":
				case "ReferenceSequence":
					flidType = CellarPropertyType.ReferenceSequence;
					break;
			}
			return flidType;
		}

		protected static string GetFlidTypeAsString(CellarPropertyType flidType)
		{
			string retval;
			switch (flidType)
			{
				default:
					throw new ArgumentException("Property element name not recognized.");
				case CellarPropertyType.Boolean:
					retval = "Boolean";
					break;
				case CellarPropertyType.Integer:
					retval = "Integer";
					break;
				case CellarPropertyType.Time:
					retval = "Time";
					break;
				case CellarPropertyType.String:
					retval = "String";
					break;
				case CellarPropertyType.MultiString:
					retval = "MultiString";
					break;
				case CellarPropertyType.Unicode:
					retval = "Unicode";
					break;
				case CellarPropertyType.MultiUnicode:
					retval = "MultiUnicode";
					break;
				case CellarPropertyType.Guid:
					retval = "Guid";
					break;
				case CellarPropertyType.Image:
					retval = "Image";
					break;
				case CellarPropertyType.GenDate:
					retval = "GenDate";
					break;
				case CellarPropertyType.Binary:
					retval = "Binary";
					break;
				case CellarPropertyType.Numeric:
					retval = "Numeric";
					break;
				case CellarPropertyType.Float:
					retval = "Float";
					break;
				case CellarPropertyType.OwningAtomic:
					retval = "OA";
					break;
				case CellarPropertyType.OwningCollection:
					retval = "OC";
					break;
				case CellarPropertyType.OwningSequence:
					retval = "OS";
					break;
				case CellarPropertyType.ReferenceAtomic:
					retval = "RA";
					break;
				case CellarPropertyType.ReferenceCollection:
					retval = "RC";
					break;
				case CellarPropertyType.ReferenceSequence:
					retval = "RS";
					break;
			}
			return retval;
		}

		/// <summary>
		/// Start the BEP.
		/// </summary>
		/// <param name="currentModelVersion">The current model version.</param>
		/// <returns>The data store's current version number.</returns>
		protected abstract int StartupInternal(int currentModelVersion);

		/// <summary>
		/// Shutdown  the BEP.
		/// </summary>
		protected abstract void ShutdownInternal();

		///// <summary>
		///// Remove the file(s) associated with the data store.
		///// </summary>
		//protected abstract void RemoveBackEnd();

		private void StartupInternalWithDataMigrationIfNeeded(IThreadedProgress progressDlg)
		{
			var currentDataStoreVersion = StartupInternal(ModelVersion);

			if (currentDataStoreVersion > ModelVersion)
				throw new FwNewerVersionException(Properties.Resources.kstidProjectIsForNewerVersionOfFw);

			if (currentDataStoreVersion != ModelVersion)
			{
				// See if migration involves real data migration(s).
				// If it does not, just update the stored version number, and keep going.
				if (!m_dataMigrationManager.NeedsRealMigration(currentDataStoreVersion, ModelVersion))
				{
					if (currentDataStoreVersion != ModelVersion)
						UpdateVersionNumber(); // Only update it, if they are different.
					// One part of the registration has been done, but do the rest now.
					// Pass an empty set to indicate that there are no deleted objects we know about.
					m_identityMap.FinishRegisteringAfterDataMigration(new HashSet<ICmObjectId>());
				}
				else
				{
					// Get going the hard way with the data migration.
					DoMigration(currentDataStoreVersion, progressDlg);
				}
			}
		}

		private void DoMigration(int currentDataStoreVersion, IThreadedProgress progressDlg)
		{
			HashSet<ICmObjectOrSurrogate> newbies;
			HashSet<ICmObjectId> goners;
			HashSet<ICmObjectOrSurrogate> dirtballs = DoMigrationBasics(currentDataStoreVersion, out goners, out newbies, progressDlg);
			Commit(newbies, dirtballs, goners);
			// In case there is a problem when we open it, we'd like to have a current database to try to repair.
			CompleteAllCommits();
		}

		private HashSet<ICmObjectOrSurrogate> DoMigrationBasics(int currentDataStoreVersion,
			out HashSet<ICmObjectId> goners, out HashSet<ICmObjectOrSurrogate> newbies, IThreadedProgress progressDlg)
		{
			// This set will be VERY large. From disassembling, we know that the constructor will set a correct initial capacity
			// only if passed an actual collection of some sort. Passing just the enumeration is therefore actually LESS
			// efficient, and may cause the large object heap to become fragmented. Please don't take the ToArray() call out
			// unless you really know what you're doing, and preferably discuss with JohnT first.
			var dtos = new HashSet<DomainObjectDTO>((from surrogate in m_identityMap.AllObjectsOrSurrogates()
				select new DomainObjectDTO(surrogate.Id.Guid.ToString(), surrogate.Classname, surrogate.XMLBytes)).ToArray());
			var dtoRepository = new DomainObjectDtoRepository(
				currentDataStoreVersion,
				dtos,
				(IFwMetaDataCacheManaged)m_mdcInternal,
				ProjectId.ProjectFolder);

			m_dataMigrationManager.PerformMigration(dtoRepository, ModelVersion, progressDlg);

			// TODO: Copy data store (ie. Backup).

			// Update data.
			// 1. Get newbies, dirtballs, and goners from repository
			var dirtballs = new HashSet<ICmObjectOrSurrogate>(new ObjectSurrogateEquater());
			var idFact = m_cache.ServiceLocator.ObjectIdFactory;
			foreach (var dirtball in dtoRepository.Dirtballs)
			{
				// Since we're doing migration, everything in the map should still be a surrogate.
				var originalSurr = m_identityMap.GetObjectOrSurrogate(idFact.FromGuid(new Guid(dirtball.Guid)))
					as CmObjectSurrogate;
				originalSurr.Reset(dirtball.Classname, dirtball.XmlBytes);
				dirtballs.Add(originalSurr);
			}
			ICmObjectIdFactory idFactory = m_cache.ServiceLocator.GetInstance<ICmObjectIdFactory>();
			// This set could be quite large. From disassembling, we know that the constructor will set a correct initial capacity
			// only if passed an actual collection of some sort. Passing just the enumeration is therefore actually LESS
			// efficient, and may cause the large object heap to become fragmented. Please don't take the ToArray() call out
			// unless you really know what you're doing, and preferably discuss with JohnT first.
			goners = new HashSet<ICmObjectId>(
				(from goner in dtoRepository.Goners select idFactory.FromGuid(new Guid(goner.Guid))).ToArray());

			// One part of the registration has been done, but do the rest now.
			// This will also remove surrogates in 'goners' from the
			// partial previous registration.
			m_identityMap.RemoveGoners(goners);
			// (Splitting the operation into two parts allows stepping over the second part
			// in the debugger when trying to write out a partial data migration.)
			m_identityMap.FinishRegisteringAfterDataMigration(new HashSet<ICmObjectId>());

			newbies = new HashSet<ICmObjectOrSurrogate>(new ObjectSurrogateEquater());
			foreach (var newbie in dtoRepository.Newbies)
			{
				var newSurr = m_surrogateFactory.Create(
					new Guid(newbie.Guid),
					newbie.Classname,
					newbie.Xml);
				RegisterInactiveSurrogate(newSurr);
				newbies.Add(newSurr);
			}
			return dirtballs;
		}

		///// <summary>
		///// Restore from a data migration, which used an XML BEP.
		///// </summary>
		///// <param name="xmlBepPathname"></param>
		//protected abstract void RestoreWithoutMigration(string xmlBepPathname);

		/// <summary>
		/// Create a LangProject with the BEP.
		/// </summary>
		protected abstract void CreateInternal();

		private void DoPort(IFwMetaDataCacheManaged mdc, IdentityMap sourceIdentityMap)
		{
			DoPortWithoutBootstrapping(false, mdc, sourceIdentityMap);
			// Instantiate core FDO objects in new system.
			BootstrapExtantSystem();
		}

		protected void RegisterInactiveSurrogate(ICmObjectSurrogate surrogate)
		{
			m_identityMap.RegisterInactiveSurrogate(surrogate);
		}

		protected void RegisterSurrogateForConversion(ICmObjectSurrogate surrogate)
		{
			m_identityMap.RegisterSurrogateForConversion(surrogate);
		}

		protected bool HaveAnythingToCommit(HashSet<ICmObjectOrSurrogate> newbies, HashSet<ICmObjectOrSurrogate> dirtballs, HashSet<ICmObjectId> goners,
			out IEnumerable<CustomFieldInfo> cfiList)
		{
			bool anyModifiedCustomFields;
			return HaveAnythingToCommit(newbies, dirtballs, goners, out anyModifiedCustomFields, out cfiList);
		}

		protected bool HaveAnythingToCommit(HashSet<ICmObjectOrSurrogate> newbies, HashSet<ICmObjectOrSurrogate> dirtballs, HashSet<ICmObjectId> goners,
			out bool anyModifiedCustomFields,
			out IEnumerable<CustomFieldInfo> cfiList)
		{
			cfiList = m_mdcInternal.GetCustomFields();
			anyModifiedCustomFields = HaveAnyModifiedCustomProperties(cfiList);
			var needsCommit = anyModifiedCustomFields
				   || newbies.Count > 0
				   || dirtballs.Count > 0
				   || goners.Count > 0;

			if (needsCommit)
				EnsureItemsInOnlyOneSet(newbies, dirtballs, goners);

			return needsCommit;
		}

		private bool HaveAnyModifiedCustomProperties(IEnumerable<CustomFieldInfo> customFieldInfos)
		{
			var oldKeys = new Dictionary<string, CustomFieldInfo>(m_extantCustomFields);
			m_extantCustomFields.Clear();
			var retval = false;
			foreach (var cfi in customFieldInfos)
			{
				var key = cfi.Key;
				m_extantCustomFields.Add(key, cfi);
				CustomFieldInfo oldInfo;
				if (!oldKeys.TryGetValue(key, out oldInfo) || !oldInfo.Equals(cfi))
					retval = true; // new or modified
				oldKeys.Remove(key);
			}
			// if there are any keys left, then they must be custom fields that were deleted
			if (!retval)
				retval = oldKeys.Count > 0;
			return retval;
		}

		/// <summary>
		/// Load the language project and the writing systems.
		/// </summary>
		protected void BootstrapExtantSystem()
		{
			EnsureWritingSystemsExist(m_cache.LanguageProject.AnalysisWss);
			EnsureWritingSystemsExist(m_cache.LanguageProject.VernWss);
			m_cache.ServiceLocator.WritingSystemManager.Save();
		}

		private void EnsureWritingSystemsExist(string wssStr)
		{
			if (string.IsNullOrEmpty(wssStr))
				return;

			IWritingSystemManager wsManager = m_cache.ServiceLocator.WritingSystemManager;
			foreach (string wsId in wssStr.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries))
			{
				IWritingSystem ws;
				wsManager.GetOrSet(wsId, out ws);
			}
		}

		private void InitializeWritingSystemManager()
		{
			// if there is no project path specified, then just use the default memory-based manager.
			// this will happen with the memory-only BEP.
			if (UseMemoryWritingSystemManager || string.IsNullOrEmpty(ProjectId.SharedProjectFolder))
				return;

			var globalStore = new GlobalFileWritingSystemStore(DirectoryFinder.GlobalWritingSystemStoreDirectory);
			string storePath = Path.Combine(ProjectId.SharedProjectFolder, DirectoryFinder.ksWritingSystemsDir);
			var wsManager = (PalasoWritingSystemManager)m_cache.ServiceLocator.WritingSystemManager;
			wsManager.GlobalWritingSystemStore = globalStore;
			wsManager.LocalWritingSystemStore = new LocalFileWritingSystemStore(storePath, globalStore);
			wsManager.TemplateFolder = DirectoryFinder.TemplateDirectory;
		}

		#region IDataSetup implementation

		/// <summary>
		/// Load domain.
		/// </summary>
		/// <param name="bulkLoadDomain">The domain to load.</param>
		public void LoadDomain(BackendBulkLoadDomain bulkLoadDomain)
		{
			try
			{
				//Debug.WriteLine("###### Starting LoadDomain on thread " + Thread.CurrentThread.Name);
				if (bulkLoadDomain == BackendBulkLoadDomain.None)
					return; // Nothing to load.

				switch (bulkLoadDomain)
				{
					case BackendBulkLoadDomain.All:
						lock (m_syncRoot)
						{
							if (m_loadedDomains.m_loadedLexicon
								&& m_loadedDomains.m_loadedScripture
								&& m_loadedDomains.m_loadedText
								&& m_loadedDomains.m_loadedWFI)
								return; // Already loaded everything.
							m_loadedDomains = new LoadedDomains(true, true, true, true);
						}
						ReconstituteObjectsFor(int.MaxValue);
						break;
					case BackendBulkLoadDomain.Lexicon:
						lock (m_syncRoot)
						{
							if (m_loadedDomains.m_loadedLexicon)
								return; // Already loaded.
							m_loadedDomains.m_loadedLexicon = true;
						}

						ReconstituteObjectsFor(LexDbTags.kClassId);
						ReconstituteObjectsFor(LexEntryTags.kClassId);
						ReconstituteObjectsFor(LexSenseTags.kClassId);
						ReconstituteObjectsFor(MoStemAllomorphTags.kClassId);
						ReconstituteObjectsFor(MoAffixAllomorphTags.kClassId);
						ReconstituteObjectsFor(MoAffixProcessTags.kClassId);
						break;
					case BackendBulkLoadDomain.Scripture:
						lock (m_syncRoot)
						{
							if (m_loadedDomains.m_loadedScripture)
								return; // Already loaded.
							m_loadedDomains.m_loadedScripture = true;
						}
						// Pre-load word form occurences so that typing in certain circumstances won't produce
						// a noticable delay which can also cause Keyman keyboards to mess up. (FWR-2205)
						IWfiWordformRepositoryInternal repository =
							(IWfiWordformRepositoryInternal)m_cache.ServiceLocator.GetInstance<IWfiWordformRepository>();
						repository.EnsureOccurrencesInTexts();

						ReconstituteObjectsFor(StStyleTags.kClassId);
						ReconstituteObjectsFor(ScrBookTags.kClassId);
						ReconstituteObjectsFor(ScrSectionTags.kClassId);
						ReconstituteObjectsFor(StTextTags.kClassId);
						ReconstituteObjectsFor(ScrTxtParaTags.kClassId);
						ReconstituteObjectsFor(CmTranslationTags.kClassId);
						ReconstituteObjectsFor(ScrFootnoteTags.kClassId);
					ReconstituteObjectsFor(ChkTermTags.kClassId);
					ReconstituteObjectsFor(ChkRefTags.kClassId);
					ReconstituteObjectsFor(ChkRenderingTags.kClassId);
						break;
					case BackendBulkLoadDomain.Text:
						lock (m_syncRoot)
						{
							if (m_loadedDomains.m_loadedText)
								return; // Already loaded.
							m_loadedDomains.m_loadedText = true;
						}

						ReconstituteObjectsFor(TextTags.kClassId);
						ReconstituteObjectsFor(StTextTags.kClassId);
						ReconstituteObjectsFor(StTxtParaTags.kClassId);
						ReconstituteObjectsFor(SegmentTags.kClassId);
						break;
					case BackendBulkLoadDomain.WFI:
						lock (m_syncRoot)
						{
							if (m_loadedDomains.m_loadedWFI)
								return; // Already loaded.
							m_loadedDomains.m_loadedWFI = true;
						}

						ReconstituteObjectsFor(WfiWordformTags.kClassId);
						ReconstituteObjectsFor(WfiAnalysisTags.kClassId);
						ReconstituteObjectsFor(WfiGlossTags.kClassId);
						ReconstituteObjectsFor(WfiMorphBundleTags.kClassId);
						break;
						//case BackendBulkLoadDomain.None: // 'On demand' loading only. Fall through.
					default:
						break; // 'On demand' loading only.
				}
			}
			finally
			{
				//Debug.WriteLine("###### Exiting LoadDomain on thread " + Thread.CurrentThread.Name);
			}
		}

		/// <summary>
		/// Loads the domain asynchronously.
		/// </summary>
		/// <param name="bulkLoadDomain">The domain to load.</param>
		public void LoadDomainAsync(BackendBulkLoadDomain bulkLoadDomain)
		{
			if (bulkLoadDomain == BackendBulkLoadDomain.None)
				return; // Nothing to load.

			m_loadDomainThreads.RemoveAll(t => !t.IsAlive);
			var thread = new Thread(param => LoadDomain((BackendBulkLoadDomain)param))
			{
				IsBackground = true,
				Priority = ThreadPriority.BelowNormal,
				Name = "Domain Loading"
			};
			m_loadDomainThreads.Add(thread);
			thread.Start(bulkLoadDomain);
		}

		/// <summary>
		/// Reconstitute all of the objects in the specified class.
		/// </summary>
		/// <param name="classId">The class ID.</param>
		private void ReconstituteObjectsFor(int classId)
		{
			if (m_stopLoadDomain)
				return;

#pragma warning disable 219
			ICmObject obj;
#pragma warning restore 219
			foreach (ICmObjectOrSurrogate surrogate in m_identityMap.AllObjectsOrSurrogates(classId))
			{
				if (m_stopLoadDomain)
					break;
				obj = surrogate.Object; // This will reconstitute it.
			}
		}

		/// <summary>
		/// Identifies the project
		/// </summary>
		public IProjectIdentifier ProjectId { get; protected set; }

		/// <summary>
		/// Get the name of a remote server where the database actually lives, or null if it's
		/// on the local machine.
		/// </summary>
		public virtual string ServerName
		{
			get { return null; }
		}

		/// <summary>
		/// Gets or sets a value indicating whether to use a memory-based writing system manager
		/// (for testing purposes).
		/// </summary>
		/// <value>
		/// if set to <c>true</c> use memory-only writing system manager (for testing purposes).
		/// </value>
		public bool UseMemoryWritingSystemManager
		{
			get; set;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Start the BEP with the given parameters.
		/// </summary>
		/// <param name="projectId">Identifies the project to load.</param>
		/// <param name="fBootstrapSystem">True to bootstrap the existing system, false to skip
		/// that step</param>
		/// <param name="progressDlg">The progress dialog box</param>
		/// ------------------------------------------------------------------------------------
		public void StartupExtantLanguageProject(IProjectIdentifier projectId, bool fBootstrapSystem,
			IThreadedProgress progressDlg)
		{
			ProjectId = projectId;
			try
			{
				StartupInternalWithDataMigrationIfNeeded(progressDlg);
				InitializeWritingSystemManager();
				if (fBootstrapSystem)
					BootstrapExtantSystem();
			}
			catch (Exception e)
			{
				// If anything unexpected goes wrong give BEP change to release any resources.
				ShutdownInternal();
				throw;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new LanguageProject for the BEP with the given parameters.
		/// </summary>
		/// <param name="projectId">Identifies the project to create.</param>
		/// ------------------------------------------------------------------------------------
		public void CreateNewLanguageProject(IProjectIdentifier projectId)
		{
			ProjectId = projectId;
			CreateInternal();
			InitializeWritingSystemManager();
			// Load basic data (i.e., writing systems and LP).
			BootstrapNewLanguageProject.BootstrapNewSystem(m_cache.ServiceLocator);
		}

		internal void RegisterOriginalCustomProperties(IEnumerable<CustomFieldInfo> originalCustomProperties)
		{
			foreach (var cfi in originalCustomProperties)
			{
				if (m_extantCustomFields.ContainsKey(cfi.Key))
					return; // Must have done a migration.
				m_extantCustomFields.Add(cfi.Key, cfi);
			}
			if (originalCustomProperties.Count() > 0)
				m_mdcInternal.AddCustomFields(originalCustomProperties);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize this data store using data from another data store (i.e., migrate from
		/// one DB to another).
		/// </summary>
		/// <param name="projectId">Identifies the project to create.</param>
		/// <param name="sourceDataStore">The source of the data.</param>
		/// <param name="userWsIcuLocale">The ICU locale of the default user WS.</param>
		/// <param name="progressDlg">The progress dialog box.</param>
		/// ------------------------------------------------------------------------------------
		public void InitializeFromSource(IProjectIdentifier projectId,
			BackendStartupParameter sourceDataStore, string userWsIcuLocale,
			IThreadedProgress progressDlg)
		{
			if (sourceDataStore == null) throw new ArgumentNullException("sourceDataStore");

			// 1. Basic creation of new system with no objects instantiated.
			ProjectId = projectId;
			CreateInternal();

			// 2. Initialize the writing system manager
			InitializeWritingSystemManager();

			// 3. Startup source BEP, but without instantiating any FDO objects (surrogates, are loaded).
			using (var sourceCache = FdoCache.CreateCacheFromExistingData(sourceDataStore.ProjectId,
				userWsIcuLocale, progressDlg))
			{
				// 4. Do the port.
				var sourceCacheServLoc = sourceCache.ServiceLocator;
				DoPort(sourceCacheServLoc.GetInstance<IFwMetaDataCacheManaged>(), sourceCacheServLoc.GetInstance<IdentityMap>());
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize this data store using data from an existing cache (i.e., migrate from
		/// one DB to another).
		/// </summary>
		/// <param name="projectId">Identifies the new project to create.</param>
		/// <param name="sourceCache">The source FDO cache. (The source data store is already opened.)</param>
		/// ------------------------------------------------------------------------------------
		public void InitializeFromSource(IProjectIdentifier projectId, FdoCache sourceCache)
		{
			if (sourceCache == null) throw new ArgumentNullException("sourceCache");

			// 1. Basic creation of new system with no objects instantiated.
			ProjectId = projectId;
			CreateInternal();

			// 2. Initialize the writing system manager
			InitializeWritingSystemManager();

			// 3. Do the port.
			DoPort(sourceCache.ServiceLocator.GetInstance<IFwMetaDataCacheManaged>(), sourceCache.ServiceLocator.GetInstance<IdentityMap>());
		}

		///// ------------------------------------------------------------------------------------
		///// <summary>
		///// Create a new LanguageProject using data from another data store
		///// (i.e., migrate from one DB to another).
		///// </summary>
		///// <param name="projectId">Identifies the new project to create.</param>
		///// <param name="sourceCache">The source of the data.
		///// (The source data store is assumed to be just barely opened with nothing reconstituted.)</param>
		///// <param name="portVersion">The model version that should be used on the output.</param>
		///// <remarks>
		///// NB: This method *does not* do any data migration(s) on data. It is raw data movement only.
		///// Use InitializeFromSource to include data migration(s) while moving to another data storage device.
		///// </remarks>
		///// ------------------------------------------------------------------------------------
		//public void PortLanguageProject(IProjectIdentifier projectId, FdoCache sourceCache, int portVersion)
		//{
		//    if (sourceCache == null) throw new ArgumentNullException("sourceCache");

		//    m_modelVersionOverride = portVersion;

		//    // 1. Basic creation of new system with no objects instantiated.
		//    CreateInternal(projectId);

		//    // 2. Initialize the writing system manager
		//    InitializeWritingSystemManager();

		//    var sourceCacheServLoc = sourceCache.ServiceLocator;
		//    DoPortWithoutBootstrapping(true, sourceCacheServLoc.GetInstance<IFwMetaDataCacheManaged>(), sourceCacheServLoc.GetInstance<IdentityMap>());
		//}

		private void DoPortWithoutBootstrapping(bool doingConversion, IFwMetaDataCacheManaged mdc, IdentityMap sourceIdentityMap)
		{
			if (mdc == null) throw new ArgumentNullException("mdc");
			if (sourceIdentityMap == null) throw new ArgumentNullException("sourceIdentityMap");

			// Port optional custom fields.
			m_mdcInternal.AddCustomFields(((IFwMetaDataCacheManagedInternal)mdc).GetCustomFields());

			// Port data.
			var newbies = new HashSet<ICmObjectOrSurrogate>(new ObjectSurrogateEquater());
			foreach (var sourceSurrogate in sourceIdentityMap.AllObjectsOrSurrogates())
			{
				var targetSurrogate = m_surrogateFactory.Create(sourceSurrogate);

				if (doingConversion)
					RegisterSurrogateForConversion(targetSurrogate);
				else
					RegisterInactiveSurrogate(targetSurrogate);
				newbies.Add(targetSurrogate);
			}

			Commit(newbies, new HashSet<ICmObjectOrSurrogate>(new ObjectSurrogateEquater()), new HashSet<ICmObjectId>());
		}

		///// <summary>
		///// Create a new LanguageProject using data from another data store
		///// (i.e., migrate from one DB to another).
		///// </summary>
		///// <param name="projectId">Identifies the new project to create.</param>
		///// <param name="sourceDataStore">The source of the data. (The source data store is assumed to *not* be opened.)</param>
		///// <remarks>
		///// NB: This method *does not* do any data migration(s) on data. It is raw data movement only.
		///// Use InitializeFromSource to include data migration(s) while moving to another data storage device.
		///// </remarks>
		//public void PortLanguageProject(IProjectIdentifier projectId, BackendStartupParameter sourceDataStore)
		//{
		//    throw new NotImplementedException();
		//}

		///// <summary>
		///// Restore system from a backup version.
		///// </summary>
		///// <param name="projectId">Identifies the new project to create.</param>
		///// <param name="backupDataStore">Backup infromation. (May well be in a different data store format.)</param>
		///// <remarks>
		///// This may require data migration.
		///// </remarks>
		//public void RestoreLanguageProjectFromBackup(IProjectIdentifier projectId, BackendStartupParameter backupDataStore)
		//{
		//    throw new NotImplementedException();
		//}

		/// <summary>
		/// Rename the database. The implementation will be different for each
		/// database and file type. Make the underlying data files have the given
		/// basename.
		/// </summary>
		/// <param name="sNewBasename"></param>
		/// <returns>true if the rename succeeds, false if it fails</returns>
		// RenameDatabase originally called RenameMyFiles. This makes sense
		// for the XML back end, and perhaps for Berkeley, but it does not make sense
		// for most database engines. These engines typically know more about their
		// files, and have specific ways of manipulating those files.
		// MySQL in particular gets confused if you monkey directly with file names,
		// because each table is a separate file, and the database name is a directory.
		public abstract bool RenameDatabase(string sNewBasename);

		#endregion IDataSetup implementation

		#region IDataReader implementation

		/// <summary>
		/// Get the CmObject for the given Guid. (Deprecated...try to use the ICmObject version.)
		/// </summary>
		/// <param name="guid">The Guid of the object to return</param>
		/// <returns>The CmObject that has the given Guid.</returns>
		/// <exception cref="T:System.Collections.Generic.KeyNotFoundException">Thrown when the given Guid is not in the dictionary.</exception>
		public ICmObject GetObject(Guid guid)
		{
			var obj = m_identityMap.GetObject(guid);

			// Filter out objects that are new or deleted.
			return obj.IsValidObject ? obj : null;
		}

		/// <summary>
		/// Get the CmObject for the given Guid.
		/// </summary>
		/// <param name="guid">The Guid of the object to return</param>
		/// <returns>The CmObject that has the given Guid.</returns>
		/// <exception cref="KeyNotFoundException">Thrown when the given Guid is not in the dictionary.</exception>
		public ICmObject GetObject(ICmObjectId guid)
		{
			var obj = m_identityMap.GetObject(guid);

			// Added assert as way to track down problem reported in FWR-3107. If this becomes a problem
			// in other cases it can be removed. The problem was that this was sometimes returning a null
			// because of a race condition in the IsValidObject method.
			Debug.Assert(obj.IsValidObject, "Invalid object returned");

			// Filter out objects that are new or deleted.
			return obj.IsValidObject ? obj : null;
		}

		/// <summary>
		/// If the identity map for this ID contains a CmObject, return it.
		/// </summary>
		public ICmObject GetObjectIfFluffed(ICmObjectId id)
		{
			return m_identityMap.GetObjectIfFluffed(id);
		}
		/// <summary>
		/// Get the CmObject for the given Hvo.
		/// </summary>
		/// <param name="hvo">The Hvo of the object to return</param>
		/// <returns>The CmObject that has the given Hvo.</returns>
		/// <exception cref="T:System.Collections.Generic.KeyNotFoundException">Thrown when the given Hvo is not in the dictionary.</exception>
		public ICmObject GetObject(int hvo)
		{
			return m_identityMap.GetObject(hvo);
		}

		/// <summary>
		/// Attempts to get the CmObject for the given HVO.
		/// </summary>
		/// <param name="hvo">The HVO of the object to get</param>
		/// <param name="obj">The CmObject that has the given HVO, or null if it could not be found</param>
		/// <returns>True if the an object with the specified HVO was found, false otherwise.</returns>
		public bool TryGetObject(int hvo, out ICmObject obj)
		{
			return m_identityMap.TryGetObject(hvo, out obj);
		}

		/// <summary>
		/// Attempts to get the CmObject for the given Guid.
		/// </summary>
		/// <param name="guid">The Guid of the object to get</param>
		/// <param name="obj">The CmObject that has the given Guid, or null if it could not be found</param>
		/// <returns>True if the an object with the specified Guid was found, false otherwise.</returns>
		public bool TryGetObject(Guid guid, out ICmObject obj)
		{
			return m_identityMap.TryGetObject(guid, out obj);
		}

		/// <summary>
		/// Return true if the provider knows about this object.
		/// </summary>
		public bool HasObject(int hvo)
		{
			return m_identityMap.HasObject(hvo);
		}

		/// <summary>
		/// Return true if the provider knows about this object.
		/// </summary>
		public bool HasObject(Guid guid)
		{
			return m_identityMap.HasObject(guid);
		}

		/// <summary>
		/// Get all instances of the given clsid and its subclasses.
		/// </summary>
		/// <param name="clsid">The class id for the class of objects (and its subclasses) to return</param>
		/// <returns>Zero, or more, instances of the given class (or subclass).</returns>
		/// <exception cref="T:System.Collections.Generic.KeyNotFoundException">Thrown when the given class id is not in the dictionary.</exception>
		/// <exception cref="T:System.InvalidCastException">Thrown if any of the items in the list corresponding to
		/// the specified clsid can not be cast to the specified type.</exception>
		public IEnumerable<T> AllInstances<T>(int clsid) where T : ICmObject
		{
			// We need the ToArray() because fluffing up the objects using .Object modifies the collections
			// that AllObjectsOrSurrogates is built on.
			return (from surrogate in m_identityMap.AllObjectsOrSurrogates(clsid).ToArray()
				where surrogate.Object.IsValidObject
				// Filter out objects that are new or deleted.
				select (T)surrogate.Object);
		}

		/// <summary>
		/// Get all instances of a particular class as ICmObjects
		/// </summary>
		public IEnumerable<ICmObject> AllInstances(int clsid)
		{
			return (from surrogate in m_identityMap.AllObjectsOrSurrogates(clsid).ToArray()
					where surrogate.Object.IsValidObject
					// Filter out objects that are new or deleted.
					select surrogate.Object);
		}

		/// <summary>
		/// Get the next higher Hvo.
		/// </summary>
		/// <remarks>
		/// This property is not to be called except on object creation.
		/// The objects can be newly created or re-created,
		/// as is the case for loading the XML data.
		/// </remarks>
		public int GetNextRealHvo()
		{
			return m_identityMap.GetNextRealHvo();
		}

		public int GetOrAssignHvoFor(ICmObjectId id)
		{
			return m_identityMap.GetOrAssignHvoFor(id);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the counts of objects in the repository having the specified CLSID.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Count(int clsid)
		{
			return (from surrogate in m_identityMap.AllObjectsOrSurrogates(clsid)
					// Filter out objects that are new or deleted.
					where !surrogate.HasObject || surrogate.Object.IsValidObject
					select surrogate).Count();
		}

		public ICmObjectOrId GetObjectOrIdWithHvoFromGuid(Guid guid)
		{
			return m_identityMap.GetObjectOrIdWithHvoFromGuid(guid);
		}

		/// <summary>
		/// Get the HVO associatd with the given ID or object. May actually create the
		/// association, though it is more normal for it to be created in a call to
		/// GetObjectOrIdWithHvoFromGuid, or simply when the CmObject is created.
		/// </summary>
		public int GetHvoFromObjectOrId(ICmObjectOrId id)
		{
			return ((ICmObjectOrIdInternal)id).GetHvo(m_identityMap);
		}

		#endregion IDataReader implementation

		#region IDataStorer implementation

		/// <summary>
		/// Update the backend store.
		/// </summary>
		/// <param name="newbies">The newly created objects</param>
		/// <param name="dirtballs">The recently modified objects</param>
		/// <param name="goners">The recently deleted objects</param>
		/// <remarks>
		/// Subclasses should call this *after* doing everything to their data store.
		/// This will remove the temporary XML string to save memory,
		/// and remove some things from some maps (not all maps).
		/// An object will only be in one of the parameters, never more than one.
		/// </remarks>
		public virtual bool Commit(HashSet<ICmObjectOrSurrogate> newbies, HashSet<ICmObjectOrSurrogate> dirtballs, HashSet<ICmObjectId> goners)
		{
			m_identityMap.FinishUnregisteringObjects();

			return true;
		}

		/// <summary>
		/// Finish any background Commits that may be in progress.
		/// Subclasses should override if they use a background process to do writes.
		/// </summary>
		public virtual void CompleteAllCommits()
		{

		}

		/// <summary>
		/// Make sure the surrogates are only in one set.
		/// </summary>
		/// <param name="newbies"></param>
		/// <param name="dirtballs"></param>
		/// <param name="goners"></param>
		/// <remarks>
		/// This method should be called by each subclass that actually persists data,
		/// to ensure a surrogate is only in one set (and the right set, if it is in more than one).
		/// </remarks>
		private void EnsureItemsInOnlyOneSet(HashSet<ICmObjectOrSurrogate> newbies, HashSet<ICmObjectOrSurrogate> dirtballs, HashSet<ICmObjectId> goners)
		{
			var temp = newbies.ToArray(); // copy because loop may modify newbies.
			foreach (var newbie in temp)
			{
				dirtballs.Remove(newbie); // New trumps dirty.
				if (!goners.Contains(newbie.Id)) continue;

				// Created and nuked in same business transaction,
				// so don't bother messing with it.
				newbies.Remove(newbie);
				goners.Remove(newbie.Id);
			}
			foreach (var goner in goners)
				dirtballs.Remove(new IdSurrogateWrapper(goner)); // Deletion trumps dirty.
		}

		#endregion IDataStorer implementation

		/// <summary>
		/// Update the version number.
		/// </summary>
		protected abstract void UpdateVersionNumber();
	}

	/// <summary>
	/// Convert between utf-8 xml string and byte array.
	/// </summary>
	internal static class XmlByteConversionService
	{
		private static readonly UTF8Encoding m_utf8 = new UTF8Encoding(false);

		/// <summary>
		/// Convert UTF-8 xml string to a byte array.
		/// </summary>
		/// <param name="xml">xml string in UTF-8</param>
		/// <returns>Byte array of the input string</returns>
		internal static byte[] GetBytes(string xml)
		{
			return m_utf8.GetBytes(xml);
		}

		/// <summary>
		/// Convert the byte array into a UTF-8 xml string.
		/// </summary>
		/// <param name="xml"></param>
		/// <returns></returns>
		internal static string GetString(byte[] xml)
		{
			return m_utf8.GetString(xml);
		}
	}

	internal struct LoadedDomains
	{
		internal bool m_loadedWFI;
		internal bool m_loadedLexicon;
		internal bool m_loadedText;
		internal bool m_loadedScripture;

		internal LoadedDomains(bool loadedWFI, bool loadedLexicon, bool loadedText, bool loadedScripture)
		{
			m_loadedWFI = loadedWFI;
			m_loadedLexicon = loadedLexicon;
			m_loadedText = loadedText;
			m_loadedScripture = loadedScripture;
		}
	}
}
