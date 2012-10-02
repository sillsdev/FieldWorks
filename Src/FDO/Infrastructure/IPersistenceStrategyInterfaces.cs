using System;
using System.Collections.Generic;
using SIL.Utils;

namespace SIL.FieldWorks.FDO.Infrastructure
{
	/// <summary>
	/// Public interface that allows BEPs to backup, restore, rename,
	/// enumerate data stores, and initialize before loading
	/// on a specific data  store.
	/// </summary>
	public interface IDataSetup : IFWDisposable
	{
		/// <summary>
		/// Load domain.
		/// </summary>
		/// <param name="bulkLoadDomain">The domain to load.</param>
		void LoadDomain(BackendBulkLoadDomain bulkLoadDomain);

		/// <summary>
		/// Loads the domain asynchronously.
		/// </summary>
		/// <param name="bulkLoadDomain">The domain to load.</param>
		void LoadDomainAsync(BackendBulkLoadDomain bulkLoadDomain);

		/// <summary>
		/// Gets the project identifier
		/// </summary>
		IProjectIdentifier ProjectId { get; }

		/// <summary>
		/// Gets or sets a value indicating whether to use a memory-based writing system manager
		/// (for testing purposes).
		/// </summary>
		/// <value>if set to <c>true</c> use memory-only writing system manager (for testing purposes).</value>
		bool UseMemoryWritingSystemManager { get; set; }

		/// <summary>
		/// Start the BEP with the given parameters.
		/// </summary>
		/// <param name="projectId">Identifies the project to load.</param>
		/// <param name="fBootstrapSystem">True to bootstrap the existing system, false to skip that
		/// step</param>
		void StartupExtantLanguageProject(IProjectIdentifier projectId, bool fBootstrapSystem);

		/// <summary>
		/// Create a new LanguageProject for the BEP with the given parameters.
		/// </summary>
		/// <param name="projectId">Identifies the project to create.</param>
		void CreateNewLanguageProject(IProjectIdentifier projectId);

		/// <summary>
		/// Initialize this data store using data from another data store (i.e., migrate from
		/// one DB to another).
		/// </summary>
		/// <param name="projectId">Identifies the project to create.</param>
		/// <param name="sourceDataStore">The source of the data. (The source data store is assumed to *not* be opened.)</param>
		/// <param name="userWsIcuLocale">The ICU locale of the default user WS.</param>
		/// <param name="threadHelper">The thread helper used for invoking actions on the main
		/// UI thread.</param>
		/// <remarks>
		/// NB: This method *does* do any data migration(s) on the source data, if needed.
		/// Use PortLanguageProject to move data to another storage device with *no* data migration(s).
		/// </remarks>
		void InitializeFromSource(IProjectIdentifier projectId, BackendStartupParameter sourceDataStore,
			string userWsIcuLocale, ThreadHelper threadHelper);

		/// <summary>
		/// Initialize this data store using data from an existing cache (i.e., migrate from
		/// one DB to another).
		/// </summary>
		/// <param name="projectId">Identifies the project to create.</param>
		/// <param name="sourceCache">The source FDO cache. (The source data store is already opened.)</param>
		void InitializeFromSource(IProjectIdentifier projectId, FdoCache sourceCache);

		///// <summary>
		///// Create a new LanguageProject using data from another data store
		///// (i.e., migrate from one DB to another).
		///// </summary>
		///// <param name="projectId">Identifies the project to create.</param>
		///// <param name="sourceCache">
		///// The source of the data.
		///// (The source data store is assumed to be just barely opened with nothing reconstituted.)</param>
		///// <param name="portVersion">The model version that should be used on the output.</param>
		///// <remarks>
		///// NB: This method *does not* do any data migration(s) on data. It is raw data movement only.
		///// Use InitializeFromSource to include data migration(s) while moving to another data storage device.
		///// </remarks>
		//void PortLanguageProject(IProjectIdentifier projectId, FdoCache sourceCache, int portVersion);

		///// <summary>
		///// Create a new LanguageProject using data from another data store
		///// (i.e., migrate from one DB to another).
		///// </summary>
		///// <param name="projectId">Identifies the project to create.</param>
		///// <param name="sourceDataStore">The source of the data. (The source data store is assumed to *not* be opened.)</param>
		///// <remarks>
		///// NB: This method *does not* do any data migration(s) on data. It is raw data movement only.
		///// Use InitializeFromSource to include data migration(s) while moving to another data storage device.
		///// </remarks>
		//void PortLanguageProject(IProjectIdentifier projectId, BackendStartupParameter sourceDataStore);

		///// <summary>
		///// Restore system from a backup version.
		///// </summary>
		///// <param name="projectId">Identifies the project to create.</param>
		///// <param name="backupDataStore">Backup information. (May well be in a different data store format.)</param>
		///// <remarks>
		///// This may require data migration.
		///// </remarks>
		//void RestoreLanguageProjectFromBackup(IProjectIdentifier projectId, BackendStartupParameter backupDataStore);

		/// <summary>
		/// Rename the database, including the underlying data files, have the given basename.
		/// </summary>
		/// <param name="sNewBasename"></param>
		/// <returns>true if the rename succeeds, false if it fails</returns>
		bool RenameDatabase(string sNewBasename);
	}

	/// <summary>
	/// This interface defines ways to read data from some data storage device.
	/// </summary>
	internal interface IDataReader : IFWDisposable
	{
		/// <summary>
		/// Get the CmObject for the given Guid.
		/// </summary>
		/// <param name="guid">The Guid of the object to return</param>
		/// <returns>The CmObject that has the given Guid.</returns>
		/// <exception cref="KeyNotFoundException">Thrown when the given Guid is not in the dictionary.</exception>
		ICmObject GetObject(Guid guid);

		/// <summary>
		/// Get the CmObject for the given Guid.
		/// </summary>
		/// <exception cref="KeyNotFoundException">Thrown when the given Guid is not in the dictionary.</exception>
		ICmObject GetObject(ICmObjectId id);

		/// <summary>
		/// Get the CmObject for the given Hvo.
		/// </summary>
		/// <param name="hvo">The Hvo of the object to return</param>
		/// <returns>The CmObject that has the given Hvo.</returns>
		/// <exception cref="KeyNotFoundException">Thrown when the given Hvo is not in the dictionary.</exception>
		ICmObject GetObject(int hvo);

		/// <summary>
		/// If the identity map for this ID contains a CmObject, return it.
		/// </summary>
		ICmObject GetObjectIfFluffed(ICmObjectId id);

		/// <summary>
		/// Return true if GetObject(hvo) will return a value (that is, if this returns true, GetObject will not throw
		/// a KeyNotFoundException).
		/// </summary>
		/// <param name="hvo"></param>
		/// <returns></returns>
		bool HasObject(int hvo);

		/// <summary>
		/// Return true if GetObject(guid) will return a value (that is, if this returns true, GetObject will not throw
		/// a KeyNotFoundException).
		/// </summary>
		/// <param name="guid"></param>
		/// <returns></returns>
		bool HasObject(Guid guid);

		/// <summary>
		/// Attempts to get the CmObject for the given HVO.
		/// </summary>
		/// <param name="hvo">The HVO of the object to get</param>
		/// <param name="obj">The CmObject that has the given HVO, or null if it could not be found</param>
		/// <returns>True if the an object with the specified HVO was found, false otherwise.</returns>
		bool TryGetObject(int hvo, out ICmObject obj);

		/// <summary>
		/// Attempts to get the CmObject for the given Guid.
		/// </summary>
		/// <param name="guid">The Guid of the object to get</param>
		/// <param name="obj">The CmObject that has the given Guid, or null if it could not be found</param>
		/// <returns>True if the an object with the specified Guid was found, false otherwise.</returns>
		bool TryGetObject(Guid guid, out ICmObject obj);

		/// <summary>
		/// Get all instances of the given clsid and its subclasses.
		/// </summary>
		/// <param name="clsid">The class id for the class of objects (and its subclasses) to return</param>
		/// <returns>Zero, or more, instances of the given class (or subclass).</returns>
		/// <exception cref="KeyNotFoundException">Thrown when the given class id is not in the dictionary.</exception>
		/// <exception cref="InvalidCastException">Thrown if any of the items in the list corresponding to
		/// the specified clsid can not be cast to the specified type.</exception>
		IEnumerable<T> AllInstances<T>(int clsid) where T : ICmObject;
		/// <summary>
		///  Get all instances of a particular class as ICmObjects
		/// </summary>
		IEnumerable<ICmObject> AllInstances(int clsid);

		/// <summary>
		/// Get the next higher Hvo.
		/// </summary>
		/// <remarks>
		/// This property is not to be called except on object creation.
		/// The objects can be newly created or re-created,
		/// as is the case for loading the XML data.
		/// </remarks>
		int GetNextRealHvo();

		/// <summary>
		/// This usually returns GetNextRealHvo(), but sometimes we assign an HVO to an ID
		/// without fluffing up the object first.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		int GetOrAssignHvoFor(ICmObjectId id);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the counts of objects in the repository having the specified CLSID.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		int Count(int clsid);

		/// <summary>
		/// This method should be used when it is desirable to have HVOs associated with
		/// Guids for objects which may not yet have been fluffed up. The ObjectOrId may be
		/// passed to GetHvoFromObjectOrId to get an HVO; anything that actually uses
		/// the HVO will result in the object being fluffed, but that can be delayed (e.g.,
		/// when persisting a pre-sorted list of guids).
		/// </summary>
		ICmObjectOrId GetObjectOrIdWithHvoFromGuid(Guid guid);

		/// <summary>
		/// Get the HVO associatd with the given ID or object. May actually create the
		/// association, though it is more normal for it to be created in a call to
		/// GetObjectOrIdWithHvoFromGuid.
		/// </summary>
		int GetHvoFromObjectOrId(ICmObjectOrId id);

	}

	/// <summary>
	/// Persist the given objects.
	/// This may be create new ones, modify previously persisted ones,
	/// or delete presiously persisted ones.
	/// </summary>
	/// <remarks>
	/// Implementors should not assume an object to be in only one of the given sets,
	/// so they should take steps to ensure that only one operation is performed for
	/// any given surrogate.
	/// </remarks>
	internal interface IDataStorer : IFWDisposable
	{
		bool Commit(HashSet<ICmObjectOrSurrogate> newbies, HashSet<ICmObjectOrSurrogate> dirtballs, HashSet<ICmObjectId> goners);
		void CompleteAllCommits();
	}

	/// <summary>
	/// Additional functionality implemented by a client-server backend.
	/// </summary>
	internal interface IClientServerDataManager
	{
		/// <summary>
		/// Get any changes we haven't previously seen which other clients have made to our database.
		/// </summary>
		/// <param name="foreignNewbies">New objects created on the other client</param>
		/// <param name="foreignDirtballs">Object we know about modified on the other client.</param>
		/// <param name="foreignGoners">objects we know about deleted by the other client.</param>
		/// <param name="fGetCommitLock">if true, we intend to Commit; if there are no conflicts, lock
		/// until commit. (If there are conflicts, we don't lock till we reconciled.)</param>
		/// <returns>true if there are any foreign changes</returns>
		bool GetUnseenForeignChanges(out List<ICmObjectSurrogate> foreignNewbies,
			out List<ICmObjectSurrogate> foreignDirtballs,
			out List<ICmObjectId> foreignGoners, bool fGetCommitLock);

		/// <summary>
		/// Return a string (typically at or near shutdown) which may be passed back to NewObjectsSinceVersion.
		/// </summary>
		string VersionStamp { get; }

		/// <summary>
		/// Pass as versionStamp a string previously obtained from VersionStamp. Answer true if changes saved to
		/// the database since versionStamp included the creation of new objects of class classname.
		/// If the backend cannot be sure, it should answer true; this method is used to suppress use of locally
		/// persisted lists as an optimization.
		/// </summary>
		bool NewObjectsSinceVersion(string versionStamp, string classname);
	}
}
