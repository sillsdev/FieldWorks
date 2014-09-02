// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: Db4oClientServerBackendProvider.cs
// Responsibility: Steve Miller
// Last reviewed: Never
// --------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Remoting;
using System.Threading;
using Db4objects.Db4o;
using Db4objects.Db4o.Config;
using Db4objects.Db4o.Config.Encoding;
using Db4objects.Db4o.CS;
using Db4objects.Db4o.CS.Config;
using Db4objects.Db4o.Linq;
using FwRemoteDatabaseConnector;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.DomainServices.DataMigration;
using SIL.Utils;

namespace SIL.FieldWorks.FDO.Infrastructure.Impl
{
	internal class Db4oClientServerBackendProvider : ClientServerBackendProvider
	{
		#region Configuration constants
		private const int kActivationDepth = 2;
		private const int kUpdateDepth = 2;
		private const int kBTreeNodeSize = 50;
		private const int kBlockSize = 8;
		private const bool kfDetectSchemaChanges = true;
		private const string kUser = "db4oUser";
		private const string kPassword = "db4oPassword";
		// TODO (SteveMiller): Put in more constants for configuration.
		#endregion

		#region Data members
		internal IObjectServer m_databaseServer;

		internal string m_host = Db4OLocalClientServerServices.kLocalService;
		// Ports are either passed as arguments or requested from the service.
		internal int m_port;
		internal IServerConfiguration m_serverConfig;
		internal IClientConfiguration m_clientConfig;
		//internal IExtClient m_client;
		internal IObjectContainer m_dbStore;
		// Currently obsolete flag tracks whether we are running using "localhost", that is, on the machine
		// that actually has the database...though it may not get set if we just chose that host in the
		// open dialog. Currently unused.
		internal bool m_localHost;
		private ModelVersionNumber m_modelVersionNumber;
		// For each ID, stores the db4o internal ID of the object.
		private readonly Dictionary<ICmObjectId, long> m_idMap = new Dictionary<ICmObjectId, long>();
		internal readonly Dictionary<string, CustomFieldInfo> m_myKnownCustomFields = new Dictionary<string, CustomFieldInfo>();
		// The last write generation we are sure we have seen all the changes from.
		private int m_lastWriteGenerationSeen;
		/// <summary>
		/// The value to put in CommitData.Source to identify this writer. It's arbitrary because we only
		/// care about writes after this backend was created, and only want to eliminate the ones we made ourself.
		/// </summary>
		private readonly Guid m_mySourceTag = Guid.NewGuid();

		/// <summary>
		/// This flag is needed for renaming the database files, and then reconnecting to the
		/// database.  We don't need/want to reload the data in that case!
		/// </summary>
		bool m_restarting;
		#endregion

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="identityMap"></param>
		/// <param name="surrogateFactory"></param>
		/// <param name="mdc"></param>
		/// <param name="dataMigrationManager"></param>
		/// <param name="ui"></param>
		/// <param name="dirs"></param>
		public Db4oClientServerBackendProvider(
			FdoCache cache,
			IdentityMap identityMap,
			ICmObjectSurrogateFactory surrogateFactory,
			IFwMetaDataCacheManagedInternal mdc,
			IDataMigrationManager dataMigrationManager,
			IFdoUI ui,
			IFdoDirectories dirs)
			: base(cache, identityMap, surrogateFactory, mdc, dataMigrationManager, ui, dirs)
		{
		}

		/// <summary>
		/// Obtain the next generation we will write.
		/// </summary>
		private int NextWriteGeneration
		{
			get
			{
				if (!m_dbStore.Ext().SetSemaphore("WriteGeneration", 3000))
				{
					throw new ApplicationException("The WriteGeneration semaphore seems to be permanently locked");
				}
				var generations = m_dbStore.Query<WriteGeneration>();
				WriteGeneration generation;
				if (generations.Count > 0)
				{
					generation = generations[0];
					m_dbStore.Ext().Refresh(generation, Int32.MaxValue);
				}
				else
					generation = new WriteGeneration();
				generation.Generation++;
				m_dbStore.Store(generation);
				m_dbStore.Commit(); // others need to see the updated generation!
				m_dbStore.Ext().ReleaseSemaphore("WriteGeneration");
				return generation.Generation;
			}
		}

		internal bool Lock(string id)
		{
			return m_dbStore.Ext().SetSemaphore(id, 500);
		}

		internal void Unlock(string id)
		{
			m_dbStore.Ext().ReleaseSemaphore(id);
		}

		/// <summary>
		/// internal static start method that should only be used to start a db4o server.
		/// This is is only and should only be used by the remote database service. (FwRemoteDatabaseConnectorService)
		/// </summary>
		/// <param name="filename"></param>
		/// <param name="startupPort"></param>
		/// <returns>The db4o server instance which has been started.</returns>
		internal static IObjectServer StartLocalDb4oClientServer(string filename, int startupPort)
		{
			Debug.Assert(!string.IsNullOrEmpty(filename));
			Debug.Assert(startupPort != default(int));

			IServerConfiguration serverConfig;
			return OpenServerFileInternal(null, null, filename, startupPort, out serverConfig);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Start up the back end.
		/// </summary>
		/// <param name="currentModelVersion">The current model version.</param>
		/// <returns>The data store's current version number.</returns>
		/// ------------------------------------------------------------------------------------
		protected override int StartupInternal(int currentModelVersion)
		{
			BasicInit();

			// Ask Service for connection port.
			RequestDatabaseStart();

			// Currently we don't support local-client mode.
			//if (m_localHost)
			//{
			//	OpenServerFile();
			//	StartClientOnLocalServer();
			//}
			//else
				StartClientOnRemoteServer();

			// DB4O always returns current state of objects when queries are done - need to use
			// lock so we can get a consistent state for starting.

			GetCommitLock(true);

			try
			{
				//--( Load model version number, and check against current number. )--//
				m_modelVersionNumber = m_dbStore.Query<ModelVersionNumber>()[0];
				var currentDataStoreVersion = m_modelVersionNumber.m_modelVersionNumber;
				var needConversion = (currentDataStoreVersion != currentModelVersion);

				if (m_restarting)
				{
					Debug.Assert(currentDataStoreVersion == currentModelVersion);
					m_restarting = false;		// need to set explicitly before each call.
					return currentDataStoreVersion;
				}

				//--( Load custom fields )--/

				var customFields = new List<CustomFieldInfo>();
				foreach (CustomFieldInfo customField in m_dbStore.Query<CustomFieldInfo>().ToArray())
				{
					CustomFieldInfo dupInfo;
					if (m_myKnownCustomFields.TryGetValue(customField.Key, out dupInfo))
					{
						// This should never happen, but somehow some databases are around which have the problem.
						// Try to correct it.  (The flids may not match exactly.  See LT-11486.)
						if (dupInfo.AlmostEquals(customField))
						{
							m_dbStore.Delete(customField);
							m_dbStore.Commit();
							continue;
						}
						// If they are NOT (almost) equal, we will go ahead and crash :-<
					}
					customFields.Add(customField);
					m_myKnownCustomFields.Add(customField.Key, customField);
				}
				RegisterOriginalCustomProperties(customFields);

				//--( Load surrogates )--/
				var generations = from CommitData cd in m_dbStore select cd.WriteGeneration;
				m_lastWriteGenerationSeen = 0;
				if (generations.Count() > 0)
					m_lastWriteGenerationSeen = generations.Max();

				Db4oServerInfo info = GetDb4OServerInfo(m_host, Db4OServerFinder.ServiceDiscoveryPort);
				long[] ids;
				var objs = info.GetCmObjectSurrogates(ProjectId.Name);

				using (var decompressoar = new CmObjectSurrogateStreamDecompressor(objs, m_cache, m_identityMap, m_idMap))
				{
					foreach (CmObjectSurrogate surrogate in decompressoar)
					{
						if (needConversion)
							RegisterSurrogateForConversion(surrogate);
						else
							RegisterInactiveSurrogate(surrogate);
					}
				}

				return currentDataStoreVersion;
			}
			catch (SocketException e)
			{
				throw new StartupException(string.Format(Strings.ksCannotConnectToServer, "FwRemoteDatabaseConnectorService"), e);
			}
			finally
			{
				ReleaseCommitLock();
			}

		}

		const string kCommitLock = "commit lock";

		/// <summary>
		/// Get changes we haven't seen. (This has a side effect of updating the record of which ones HAVE been seen.)
		/// This method assumes that the commit lock has already been obtained. Returns true if there are any unseen foreign changes.
		/// </summary>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="Ext() returns a reference")]
		private bool GetUnseenForeignChanges(out List<ICmObjectSurrogate> foreignNewbies,
			out List<ICmObjectSurrogate> foreignDirtballs,
			out List<ICmObjectId> foreignGoners)
		{
			foreignNewbies = new List<ICmObjectSurrogate>();
			foreignDirtballs = new List<ICmObjectSurrogate>();
			foreignGoners = new List<ICmObjectId>();

			if (m_dbStore == null)
				return false; // db has been closed.

			if (m_dbStore.Ext().IsClosed())
				return false; // db isn't open, we can't have loaded any stale data.

			// Store original write generation in case of an error.
			int startingWriteGeneration = m_lastWriteGenerationSeen;

			try
			{
				var unseenCommits = (from CommitData cd in m_dbStore
									 where cd.WriteGeneration > m_lastWriteGenerationSeen && cd.Source != m_mySourceTag
									 select cd).ToList();
				if (unseenCommits.Count == 0)
					return false;
				var idFactory = m_cache.ServiceLocator.GetInstance<ICmObjectIdFactory>();
				unseenCommits.Sort((x, y) => x.WriteGeneration - y.WriteGeneration);
				m_lastWriteGenerationSeen = unseenCommits.Last().WriteGeneration; // after sorting, so the last one is truly the greatest
				var newbies = new Dictionary<Guid, ICmObjectSurrogate>();
				var dirtballs = new Dictionary<Guid, ICmObjectSurrogate>();
				var goners = new HashSet<Guid>();
				foreach (var commitData in unseenCommits)
				{
					foreach (var goner in commitData.ObjectsDeleted)
					{
						// If it was created by a previous foreign change we haven't seen, we can just forget it.
						if (newbies.Remove(goner))
							continue;
						// If it was modified by a previous foreign change we haven't seen, we can forget the modification.
						// (but we still need to know it's gone).
						dirtballs.Remove(goner);
						goners.Add(goner);
					}
					foreach (var dirtball in commitData.ObjectsUpdated)
					{
						// This shouldn't be necessary; if a previous foreign transaction deleted it, it
						// should not show up as a dirtball in a later transaction until it has shown up as a newby.
						// goners.Remove(dirtball);
						// If this was previously known as a newby or modified, then to us it still is.
						// We already have its CURRENT data from the object itself.
						if (newbies.ContainsKey(dirtball) || dirtballs.ContainsKey(dirtball))
							continue;
						var dirtballId = idFactory.FromGuid(dirtball);
						var dirtballInfo = m_idMap[dirtballId];
						// Note that pathologically this might be null, if a later transaction has deleted it.
						var dirtballSurrogate = (ICmObjectSurrogate) m_dbStore.Ext().GetByID(dirtballInfo);
						m_dbStore.Ext().Refresh(dirtballSurrogate, 2);
						dirtballs[dirtball] = dirtballSurrogate;
					}
					foreach (var newbyDbId in commitData.ObjectsAdded)
					{
						var newObj = (ICmObjectSurrogate) m_dbStore.Ext().GetByID(newbyDbId);
						m_dbStore.Ext().Refresh(newObj, 2);
						MapId((CmObjectSurrogate)newObj);
						if (newObj == null)
							continue; // presumably a later transaction deleted it.
						var newby = newObj.Guid;
						if (goners.Remove(newby))
						{
							// an object which an earlier transaction deleted is being re-created.
							// This means that to us, it is a dirtball.
							dirtballs[newby] = newObj;
							continue;
						}
						// It shouldn't be in dirtballs; can't be new in one transaction without having been deleted previously.
						// So it really is new.
						newbies[newby] = newObj;
					}
					foreignNewbies.AddRange(newbies.Values);
					foreignDirtballs.AddRange(dirtballs.Values);
					foreignGoners.AddRange(from guid in goners select idFactory.FromGuid(guid));
				}
				return true;

			}
			catch (Db4objects.Db4o.Ext.DatabaseClosedException)
			{

				if (ResumeDb4oConnectionAskingUser())
					return GetUnseenForeignChanges(out foreignNewbies, out foreignDirtballs, out foreignGoners);

				StopClient();
				// get back to a consistant state.
				m_lastWriteGenerationSeen = startingWriteGeneration;
				throw new NonRecoverableConnectionLostException();
			}
		}

		private void ReleaseCommitLock()
		{
			try
			{
				m_dbStore.Ext().ReleaseSemaphore(kCommitLock);
			}
			catch (Db4objects.Db4o.Ext.DatabaseClosedException)
			{
				// Ignore exception.
			}
		}
		/*-------------------------------------------------------------------*/
		/// <summary>
		/// Update the backend store.
		/// </summary>
		/// <param name="newbies">The newly created objects</param>
		/// <param name="dirtballs">The recently modified objects</param>
		/// <param name="goners">The recently deleted objects</param>
		/// <returns>true if all is well (successful save or nothing to save)</returns>
		public override bool Commit(
			HashSet<ICmObjectOrSurrogate> newbies, HashSet<ICmObjectOrSurrogate> dirtballs, HashSet<ICmObjectId> goners)
		{
			if (m_dbStore == null)
			{
				if (!ResumeDb4oConnectionAskingUser())
				{
					throw new NonRecoverableConnectionLostException();
				}
			}

			if (!GetCommitLock(newbies.Count != 0 || dirtballs.Count != 0 || goners.Count != 0))
				return true;
			try
			{
				List<ICmObjectSurrogate> foreignNewbies;
				List<ICmObjectSurrogate> foreignDirtballs;
				List<ICmObjectId> foreignGoners;
				if (GetUnseenForeignChanges(out foreignNewbies, out foreignDirtballs, out foreignGoners))
				{
					IUnitOfWorkService uowService = ((IServiceLocatorInternal) m_cache.ServiceLocator).UnitOfWorkService;
					IReconcileChanges reconciler = uowService.CreateReconciler(foreignNewbies, foreignDirtballs, foreignGoners);
					if (reconciler.OkToReconcileChanges())
					{
						reconciler.ReconcileForeignChanges();
						// And continue looping, in case there are by now MORE foreign changes!
					}
					else
					{
						uowService.ConflictingChanges(reconciler);
						return true;
					}
				}

				IEnumerable<CustomFieldInfo> cfiList;
				bool anyModifiedCustomFields;
				if (!HaveAnythingToCommit(newbies, dirtballs, goners, out anyModifiedCustomFields, out cfiList))
					return true;

				var commitData = new CommitData { Source = m_mySourceTag };
				int objectAddedIndex = 0;
				commitData.ObjectsDeleted = (from item in goners select item.Guid).ToArray();

				//m_dbStore.Ext().Purge();
				var generation = commitData.WriteGeneration = NextWriteGeneration;
				// we've seen our own change, and we use a semaphore to make sure there haven't been others since we checked.
				m_lastWriteGenerationSeen = generation;
				if (anyModifiedCustomFields)
				{
					var validKeys = new HashSet<string>();
					foreach (var customFieldInfo in cfiList)
					{
						validKeys.Add(customFieldInfo.Key);
						CustomFieldInfo oldInfo;
						if (m_myKnownCustomFields.TryGetValue(customFieldInfo.Key, out oldInfo))
						{
							if (oldInfo.Equals(customFieldInfo))
								continue; // unchanged
							m_dbStore.Delete(oldInfo);
						}
						m_dbStore.Store(customFieldInfo);
						m_myKnownCustomFields[customFieldInfo.Key] = customFieldInfo;
					}
					// Get rid of deleted ones. In Db4o, it is not enough just not to re-save them!
					foreach (CustomFieldInfo customField in m_dbStore.Query<CustomFieldInfo>())
					{
						if (!validKeys.Contains(customField.Key))
							m_dbStore.Delete(customField);
					}
				}

				// The ToArray() is so we can safely modify the collection. (Note: even before we added the
				// code to convert dirtballs to newbies, there was some mysterious case where .Net thinks we have modified the
				// collection. Maybe sometimes the dirtball is present as a surrogate and modifying it by Refreshing
				// somehow changes the hashset? Anyway, be cautious about removing the ToArray(), even if you find
				// a better way to handle the spurious dirtballs).
				foreach (var dirtball in dirtballs.ToArray())
				{
					long id;
					if (!m_idMap.TryGetValue(dirtball.Id, out id))
					{
						// pathologically, SaveAndForceNewestXmlForCmObjectWithoutUnitOfWork may pass newbies as dirtballs.
						newbies.Add(dirtball);
						dirtballs.Remove(dirtball);
						continue;
					}
					var realDbObj = (CmObjectSurrogate)m_dbStore.Ext().GetByID(id);
					m_dbStore.Ext().Refresh(realDbObj, Int32.MaxValue);
					realDbObj.Update(dirtball.Classname, dirtball.XMLBytes);
					m_dbStore.Store(realDbObj);
				}

				commitData.ObjectsAdded = new long[newbies.Count]; // after possibly adjusting newbies above
				commitData.ObjectsUpdated = (from item in dirtballs select item.Id.Guid).ToArray();

				// Enhance JohnT: possibly this could be sped up by taking advantage of the case where
				// newby is already a CmObjectSurrogate. This is probably only the case where we are
				// doing data migration or switching backends, however.

				foreach (var newby in newbies)
				{
					var newSurrogate = new CmObjectSurrogate(m_cache, newby.Id, newby.Classname, newby.XMLBytes);
					m_dbStore.Store(newSurrogate);
					commitData.ObjectsAdded[objectAddedIndex++] = m_dbStore.Ext().GetID(newSurrogate);
					MapId(newSurrogate);
				}

				foreach (var goner in goners)
				{
					var id = m_idMap[goner];
					var realDbObj = (CmObjectSurrogate)m_dbStore.Ext().GetByID(id);
					m_dbStore.Ext().Refresh(realDbObj, Int32.MaxValue);
					m_dbStore.Delete(realDbObj);
					m_idMap.Remove(goner);
				}
				if (m_modelVersionOverride != m_modelVersionNumber.m_modelVersionNumber)
				{
					m_modelVersionNumber.m_modelVersionNumber = m_modelVersionOverride;
					m_dbStore.Store(m_modelVersionNumber);
				}
				m_dbStore.Store(commitData);
				m_dbStore.Commit();
			}
			catch (Db4objects.Db4o.Ext.DatabaseClosedException)
			{
				if (!ResumeDb4oConnectionAskingUser())
				{
					throw new NonRecoverableConnectionLostException();
				}

				ReleaseCommitLock();
				// reattempt the commit.
				return Commit(newbies, dirtballs, goners);
			}
			catch (Exception err)
			{
				m_dbStore.Rollback();
				throw;
			}
			finally
			{
				ReleaseCommitLock();
			}
			return base.Commit(newbies, dirtballs, goners);
		}

		/// <summary>
		/// Repeatedly ttempt to set a semaphore on the dbstore with a 3 second timeout.
		/// if we aren't to wait for a commitlock then return false after the first attempt,
		/// if we are then repeatedly show the user a message box until we can successfully set the semaphore.
		/// If we set the semaphore return true.
		/// </summary>
		/// <param name="fWaitForCommitLock"></param>
		/// <returns></returns>
		private bool GetCommitLock(bool fWaitForCommitLock)
		{
			while (!m_dbStore.Ext().SetSemaphore(kCommitLock, 3000))
			{
				if (fWaitForCommitLock)
					m_ui.DisplayMessage(MessageType.Info, Strings.ksOtherClientsAreWriting, Strings.ksShortDelayCaption, null);
				else
					return false;
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new db4o database. Removes old one if exists.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateInternal()
		{
			BasicInit();

			try
			{
				if (m_localHost)
				{
					Db4oServerInfo info = GetDb4OServerInfo(m_host, Db4OServerFinder.ServiceDiscoveryPort);
					info.CreateServerFile(ProjectId.Name);
					// Ask Service for connection port.
					RequestDatabaseStart();
				}

				StartClientOnRemoteServer();

				// Store model version number;
				m_modelVersionNumber = new ModelVersionNumber { m_modelVersionNumber = m_modelVersionOverride };
				m_dbStore.Store(m_modelVersionNumber);
				m_dbStore.Commit();
			}
			catch (SocketException e)
			{
				throw new StartupException(string.Format(Strings.ksCannotConnectToServer, "FwRemoteDatabaseConnectorService"), e);
			}
			catch (Exception err)
			{
				StopClient();
				RequestServerShutdown();
				throw;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Rename the database for storing the data.  This *might* involve four steps:
		/// 1. check that the new name(s) doesn't already exist
		/// 2. close the connection
		/// 3. rename the file(s) or directory
		/// 4. reopen the connection
		/// </summary>
		/// <param name="sNewProjectName">The new project name</param>
		/// <returns><c>true</c> if rename was successful; <c>false</c> otherwise</returns>
		/// ------------------------------------------------------------------------------------
		public override bool RenameDatabase(string sNewProjectName)
		{
			if (!ProjectId.IsLocal)
				throw new InvalidOperationException("Renaming a database needs to be done on the local machine");

			string sNewProjectFolder = Path.Combine(m_dirs.ProjectsDirectory, sNewProjectName);
			if (FileUtils.NonEmptyDirectoryExists(sNewProjectFolder))
				return false;

			// TODO (FWR-722): warn any clients or notify user of connected remote clients.
			string[] clientsThatNeedWarning = ClientServerServices.Current.Local.ListRemoteConnectedClients(ProjectId.Name);
			if (clientsThatNeedWarning.Length != 0)
			{
				// Remote clients are connected. Database can't be renamed.
				return false;
			}

			// disconnected db4o client.
			StopClient();

			// Request db4o server to close down.
			if (!RequestServerShutdown(TimeSpan.FromMilliseconds(200)))
			{
				// local client still connected or remote clients reconnected?
				return false;
			}

			string oldProjectFolder = Path.Combine(m_dirs.ProjectsDirectory, ProjectId.Name);
			string oldFile = Path.Combine(sNewProjectFolder, FdoFileHelper.GetDb4oDataFileName(ProjectId.Name));
			string newFile = Path.Combine(sNewProjectFolder, FdoFileHelper.GetDb4oDataFileName(sNewProjectName));

			try
			{
				Directory.Move(oldProjectFolder, sNewProjectFolder);
				File.Move(oldFile, newFile);
				ProjectId.Path = newFile;
			}
			catch
			{
				// attempt to go back to a consistent state.
				try { Directory.Move(sNewProjectFolder, oldProjectFolder); }
				catch
				{ }

				return false;
			}
			finally
			{
				// Re-establish the connection to the (probably renamed) files.
				m_restarting = true;
				StartupInternal(ModelVersion);
				m_stopLoadDomain = true;
			}

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Update the version number
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void UpdateVersionNumber()
		{
			m_modelVersionNumber.m_modelVersionNumber = ModelVersion;
			m_dbStore.Store(m_modelVersionNumber);
			m_dbStore.Commit();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize member variables based on what was passed in.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void BasicInit()
		{
			m_host = Db4OLocalClientServerServices.kLocalService;
			m_localHost = true;

			Debug.Assert(!ProjectId.IsLocal || !string.IsNullOrEmpty(ProjectId.Name));
			if (!string.IsNullOrEmpty(ProjectId.ServerName))
				m_host = ProjectId.ServerName;

			// Some other host name COULD be the local machine...but at present we don't really care anyway.
			m_localHost = (m_host == Db4OLocalClientServerServices.kLocalService);
		}

		/// <summary>
		/// Ask Service to start the database server.
		/// This sets the db4o connection port (m_port).
		/// </summary>
		private void RequestDatabaseStart()
		{
			try
			{
				// dynamically look up the connection port to the db4o database.
				int port;
				Exception exceptionFromStartingServer;
				Db4oServerInfo info = GetDb4OServerInfo(m_host, Db4OServerFinder.ServiceDiscoveryPort);
				if (!info.StartServer(ProjectId.Name, out port, out exceptionFromStartingServer))
				{
					// failed to start server
					throw new StartupException(String.Format(Strings.ksFailedToStartServer,
							exceptionFromStartingServer.Message), exceptionFromStartingServer);
				}
				m_port = port;
			}
			catch (SocketException e)
			{
				throw new StartupException(string.Format(Strings.ksCannotConnectToServer, "FwRemoteDatabaseConnectorService"), e);
			}
			catch (RemotingException e)
			{
				// on Mono we get a RemotingException instead of a SocketException
				throw new StartupException(string.Format(Strings.ksCannotConnectToServer, "FwRemoteDatabaseConnectorService"), e);
			}
		}

		/// <summary>
		/// ResumeDb4oConnection in responce non clean disconnection. (For example network cable unplugged
		/// or server process terminated.) This is manifested by a Db4objects.Db4o.Ext.DatabaseClosedException,
		/// when calling m_dbStore (IObjectContainer) methods.
		/// </summary>
		/// <returns>true if connection successfully resumed, otherwise false.</returns>
		private bool ResumeDb4oConnection()
		{
			try
			{
				// cleanup the db4o client connection.
				StopClient();
				// If the service has been restarted then the db4o database needs to be started.
				RequestDatabaseStart();
				// try reconnecting.
				StartClientOnRemoteServer();
			}
			catch (Exception e)
			{
				// re-connection failed.
				return false;
			}

			return true;
		}

		/// <summary>
		/// Attempts to resume the Db4o connection, repeatedly asking user if they want to retry if resuming fails.
		/// Will keep attempting to resume as many times as the users requests it.
		/// </summary>
		/// <returns>true if connection successfully resumed, otherwise false.</returns>
		private bool ResumeDb4oConnectionAskingUser()
		{
			// First attempt to reconnect without asking the user
			if (ResumeDb4oConnection())
				return true;

			// Try a few more times...after waking a laptop from sleep it may take a couple of seconds to
			// wake up the service.
			for (int i = 0; i < 10; i++)
			{
				Thread.Sleep(500);
				if (ResumeDb4oConnection())
					return true;
			}

			while (m_ui.ConnectionLost())
			{
				// if re-connection failed allow user to keep retrying as many times as they want.
				if (ResumeDb4oConnection())
					break;
			}

			if (m_dbStore == null)
				m_ui.Exit();

			// return true if connection re-established
			return (m_dbStore != null);
		}

		internal static Db4oServerInfo GetDb4OServerInfo(string host, int port)
		{
			string connectString = String.Format("tcp://{0}:{1}/FwRemoteDatabaseConnector.Db4oServerInfo", host, port);
			return (Db4oServerInfo)Activator.GetObject(typeof(Db4oServerInfo), connectString);
		}

		/// <summary>
		/// Shutdown  the BEP. This gets called from Dispose(bool)
		/// </summary>
		protected override void ShutdownInternal()
		{
			StopClient();
			RequestServerShutdown();

			// Currently we never run in server mode.
			// CloseServerFile();
		}

		/*==========================================================================================================*/
		#region db4oServer

		/// <summary>
		/// Allows getting the results of fake CmObjectSurrogate Activation done on the server.
		/// </summary>
		internal static CmObjectSurrogateTypeHandler CmObjectSurrogateTypeHandler = new CmObjectSurrogateTypeHandler();

		// Doc:
		// Network Server: http://developer.db4o.com/Documentation/Reference/db4o-7.13/net35/reference/html/reference/client-server/networked/network_server.html
		// CommonConfiguration interface: http://developer.db4o.com/documentation/reference/db4o-7.12/java/api/com/db4o/config/CommonConfiguration.html

		/// <summary>
		/// Configure the db4o server with different settings.
		/// This is is only and should only be used by the remote database service. (FwRemoteDatabaseConnectorService)
		/// </summary>
		private static IServerConfiguration ConfigureServer(FdoCache cache, IdentityMap identityMap)
		{
			// These configuration settings are based on Randy's original db4o embedded back end.

			IServerConfiguration serverConfig = Db4oClientServer.NewServerConfiguration();

			serverConfig.TimeoutServerSocket = (int)TimeSpan.FromHours(24).TotalMilliseconds;

			serverConfig.Common.RegisterTypeHandler(
				new CmObjectSurrogateTypeHandlerPredicate(),
				CmObjectSurrogateTypeHandler);
			serverConfig.Common.RegisterTypeHandler(
				new CustomFieldInfoTypeHandlerPredicate(),
				new CustomFieldInfoTypeHandler());
			serverConfig.Common.RegisterTypeHandler(
				new ModelVersionNumberTypeHandlerPredicate(),
				new ModelVersionNumberTypeHandler());

			// "A tuning hint: If callbacks are not used, you can turn this
			// feature off, to prevent db4o from looking for callback methods
			// in persistent classes. This will increase the performance on
			// system startup...In client/server environment this setting
			// should be used on both client and server. "

			serverConfig.Common.Callbacks = false;

			// "This method must be called before opening a database...
			// Performance may be improved by running db4o without using weak
			// references durring memory management at the cost of higher
			// memory consumption or by alternatively implementing a manual
			// memory management scheme using ExtObjectContainer.purge(java.lang.Object)
			// ...Setting the value to false causes db4o to use hard references
			// to objects, preventing the garbage collection process from
			// disposing of unused objects.

			// REVIEW (SteveMiller): May want to review the setting of WeakReferences
			// with Randy, and revisit the decision on setting it to false.

			serverConfig.Common.WeakReferences = false;

			// "advises db4o to try instantiating objects with/without calling
			// constructors...In client/server environment this setting should
			// be used on both client and server.

			serverConfig.Common.CallConstructors = false;

			serverConfig.Common.ActivationDepth = kActivationDepth;
			serverConfig.Common.UpdateDepth = kUpdateDepth;

			// "tuning feature: configures whether db4o checks all persistent
			// classes upon system startup, for added or removed fields. "
			//--
			// There is a db4o bugg in the serialization of the query when
			// DetectSchemaChanges = false and using QueryByExample. See:
			// http://developer.db4o.com/Forums/tabid/98/aff/4/aft/9894/afv/topic/Default.aspx#28040

			serverConfig.Common.DetectSchemaChanges = kfDetectSchemaChanges;

			// tuning feature: configures whether db4o should try to
			// instantiate one instance of each persistent class on system
			// startup.

			serverConfig.Common.TestConstructors = false;

			// The standard setting is 1 allowing for a maximum database file
			// size of 2GB. This value can be increased to allow larger
			// database files, although some space will be lost to padding
			// because the size of some stored objects will not be an exact
			// multiple of the block size. A recommended setting for large
			// database files is 8, since internal pointers have this length.

			serverConfig.File.BlockSize = kBlockSize;

			// "configures the size of BTree nodes in indexes. Default setting: 100
			// Lower values will allow a lower memory footprint and more efficient
			// reading and writing of small slots. Higher values will reduce the
			// overall number of read and write operations and allow better
			// performance at the cost of more RAM use...This setting should be
			// used on both client and server in client-server environment.

			serverConfig.Common.BTreeNodeSize = kBTreeNodeSize;

			serverConfig.Common.StringEncoding = StringEncodings.Utf8();

			// Queries need to be executed in Snapshot mode so that the results
			// aren't changed by transactions that happen while the query is
			// in progress. Details on query mode can be found at:
			// http://developer.db4o.com/Documentation/Reference/db4o-7.12/net35/reference/Content/configuration/common/query_modes.htm

			serverConfig.Common.Queries.EvaluationMode(QueryEvaluationMode.Snapshot);

			// For each of ObjectClass(type), Randy originally had .Indexed(true);

			// For surrogates we have a custom serializer so we don't need db4o trying to read
			// (or especially write) things it is connected to. And there is nothing they connect
			// to that requires cascading deletes.
			var type = typeof(CmObjectSurrogate);
			serverConfig.Common.ObjectClass(type).CascadeOnDelete(true);
			serverConfig.Common.ObjectClass(type).UpdateDepth(kUpdateDepth);
			serverConfig.Common.ObjectClass(type).MinimumActivationDepth(kActivationDepth);
			serverConfig.Common.ObjectClass(type).MaximumActivationDepth(kActivationDepth);


			type = typeof(CustomFieldInfo);
			serverConfig.Common.ObjectClass(type).CascadeOnDelete(true);
			serverConfig.Common.ObjectClass(type).UpdateDepth(kUpdateDepth);
			serverConfig.Common.ObjectClass(type).MinimumActivationDepth(kActivationDepth);
			serverConfig.Common.ObjectClass(type).MaximumActivationDepth(kActivationDepth);

			type = typeof(ModelVersionNumber);
			serverConfig.Common.ObjectClass(type).CascadeOnDelete(true);
			serverConfig.Common.ObjectClass(type).UpdateDepth(kUpdateDepth);
			serverConfig.Common.ObjectClass(type).MinimumActivationDepth(kActivationDepth);
			serverConfig.Common.ObjectClass(type).MaximumActivationDepth(kActivationDepth);

			return serverConfig;
		}

		internal static IObjectServer OpenServerFileInternal(FdoCache cache, IdentityMap identityMap, string databasePath, int port, out IServerConfiguration serverConfig)
		{
			IObjectServer databaseServer = null;
			serverConfig = null;

			try
			{
				serverConfig = ConfigureServer(cache, identityMap);
				databaseServer = Db4oClientServer.OpenServer(serverConfig, databasePath, port);
				databaseServer.GrantAccess(kUser, kPassword);
			}
			catch (Exception)
			{
				if (databaseServer != null)
				{
					databaseServer.Close();
					databaseServer.Dispose();
				}

				throw;
			}

			return databaseServer;
		}

		/*--------------------------------------------------------------------------------------------------*/
		/// <summary>
		/// Close down the server when all is said and done.
		/// </summary>
		internal void CloseServerFile()
		{
			if (m_databaseServer != null)
			{
				m_databaseServer.Close();
				m_databaseServer = null;
			}
		}

		#endregion db4oServer

		/*==========================================================================================================*/
		#region db4oClient

		/// <summary>
		/// Configure the db4o client
		/// </summary>
		// TODO (SteveMiller): Check out the CommonConfiguration interface, to
		// TODO see if it can be used for both server and client. See the link above.
		private void ConfigureClient()
		{
			// See comments in ConfigureServer() for notes about the various
			// configuration settings.

			m_clientConfig = Db4oClientServer.NewClientConfiguration();

			m_clientConfig.TimeoutClientSocket = (int)TimeSpan.FromHours(24).TotalMilliseconds;

			m_clientConfig.Common.RegisterTypeHandler(
				new CmObjectSurrogateTypeHandlerPredicate(),
				new CmObjectSurrogateTypeHandler(m_cache, m_identityMap));
			m_clientConfig.Common.RegisterTypeHandler(
				new CustomFieldInfoTypeHandlerPredicate(),
				new CustomFieldInfoTypeHandler());
			m_clientConfig.Common.RegisterTypeHandler(
				new ModelVersionNumberTypeHandlerPredicate(),
				new ModelVersionNumberTypeHandler());

			m_clientConfig.Common.Callbacks = false;
			m_clientConfig.Common.WeakReferences = false;
			m_clientConfig.Common.CallConstructors = false;
			m_clientConfig.Common.ActivationDepth = kActivationDepth;
			m_clientConfig.Common.UpdateDepth = kUpdateDepth;
			m_clientConfig.Common.DetectSchemaChanges = kfDetectSchemaChanges;
			m_clientConfig.Common.TestConstructors = false;
			m_clientConfig.Common.Queries.EvaluationMode(QueryEvaluationMode.Snapshot);
			// There is no m_clientConfig.File.Blocksize like there is for the server
			m_clientConfig.Common.BTreeNodeSize = kBTreeNodeSize;
			m_clientConfig.Common.StringEncoding = StringEncodings.Utf8();

			// For surrogates we have a custom serializer so we don't need db4o trying to read
			// (or especially write) things it is connected to. And there is nothing they connect
			// to that requires cascading deletes.
			var type = typeof(CmObjectSurrogate);
			m_clientConfig.Common.ObjectClass(type).CascadeOnDelete(true);
			m_clientConfig.Common.ObjectClass(type).UpdateDepth(kUpdateDepth);
			m_clientConfig.Common.ObjectClass(type).MinimumActivationDepth(kActivationDepth);
			m_clientConfig.Common.ObjectClass(type).MaximumActivationDepth(kActivationDepth);

			type = typeof(CustomFieldInfo);
			m_clientConfig.Common.ObjectClass(type).CascadeOnDelete(true);
			m_clientConfig.Common.ObjectClass(type).UpdateDepth(kUpdateDepth);
			m_clientConfig.Common.ObjectClass(type).MinimumActivationDepth(kActivationDepth);
			m_clientConfig.Common.ObjectClass(type).MaximumActivationDepth(kActivationDepth);

			type = typeof(ModelVersionNumber);
			m_clientConfig.Common.ObjectClass(type).CascadeOnDelete(true);
			m_clientConfig.Common.ObjectClass(type).UpdateDepth(kUpdateDepth);
			m_clientConfig.Common.ObjectClass(type).MinimumActivationDepth(kActivationDepth);
			m_clientConfig.Common.ObjectClass(type).MaximumActivationDepth(kActivationDepth);

			//m_clientConfig.Common.OutStream = new StreamWriter(@"c:\clientLog.txt");
		}

		//*--------------------------------------------------------------------------------------------------*/
		///// <summary>
		///// Start up a client that connects to a local server on this machine.
		///// </summary>
		//private void StartClientOnLocalServer()
		//{
		//    if (m_dbStore == null)
		//        m_dbStore = m_databaseServer.OpenClient();
		//}

		/*--------------------------------------------------------------------------------------------------*/
		/// <summary>
		/// Start up a client that connects to a remote server
		/// </summary>
		internal void StartClientOnRemoteServer()
		{
			if (m_dbStore == null)
			{
				ConfigureClient();
				m_dbStore = Db4oClientServer.OpenClient(m_clientConfig, m_host, m_port, kUser, kPassword);
			}
		}

		/*--------------------------------------------------------------------------------------------------*/

		/// <summary>
		/// Disconnect the client from the server
		/// </summary>
		internal void StopClient()
		{
			if (m_dbStore != null)
			{
				m_dbStore.Close();
				m_dbStore = null;
			}
		}


		/// <summary>
		/// Request that the remote db4o database be shutdown.
		/// </summary>
		/// <param name="timePreparedToWait">If server can't be shutdown immediately, retry after this length of time.</param>
		/// <returns>true if db4o database is succefully shutdown</returns>
		internal bool RequestServerShutdown(TimeSpan timePreparedToWait)
		{
			try
			{
				Db4oServerInfo info = GetDb4OServerInfo(m_host, Db4OServerFinder.ServiceDiscoveryPort);
				if (!info.StopServer(ProjectId.Name))
				{
					Thread.Sleep(timePreparedToWait);
					return info.StopServer(ProjectId.Name);
				}
			}
			catch (SocketException)
			{
				// Don't report this exception, as networking problems are likely already reported.
				return false;
			}
			catch (System.Runtime.Remoting.RemotingException)
			{
				// On Mono we get a RemotingException instead of a SocketException.
				// Don't report this exception, as networking problems are likely already reported.
				return false;
			}

			return true;
		}

		/// <summary>
		/// Request that the remote db4o database be shutdown.
		/// </summary>
		/// <returns>true if db4o database is succefully shutdown</returns>
		internal bool RequestServerShutdown()
		{
			return RequestServerShutdown(TimeSpan.Zero);
		}
		#endregion db4oClient

		/*==========================================================================================================*/

		/// <summary>
		/// Map surrogate IDs to db4o internal ID
		/// </summary>
		private void MapId(CmObjectSurrogate surrogate)
		{
			var item1 = m_dbStore.Ext().GetID(surrogate);
			m_idMap[surrogate.Id] = item1;
		}

		/// <summary>
		/// Return a string (typically at or near shutdown) which may be passed back to NewObjectsSinceVersion.
		/// Here we just use a representation of the last write generation we've seen.
		/// </summary>
		override public string VersionStamp { get { return m_lastWriteGenerationSeen.ToString(); } }

		/// <summary>
		/// Pass as versionStamp a string previously obtained from VersionStamp. Answer true if changes saved to
		/// the database since versionStamp included the creation of new objects of class classname.
		/// If the backend cannot be sure, it should answer true; this method is used to suppress use of locally
		/// persisted lists as an optimization.
		/// </summary>
		override public bool NewObjectsSinceVersion(string versionStamp, string classname)
		{
			int oldGeneration;
			if (!Int32.TryParse(versionStamp, out oldGeneration))
				return true; // if something is catastrophically wrong with the saved VersionStamp discard local cache.
			var unseenCommits = (from CommitData cd in m_dbStore
								 where cd.WriteGeneration > oldGeneration && cd.ObjectsAdded.Length > 0
								 select cd).ToList();
			if (unseenCommits.FirstOrDefault() == null)
				return false;
			var idFactory = m_cache.ServiceLocator.GetInstance<ICmObjectIdFactory>();
			foreach (var cd in unseenCommits)
			{
				foreach (var id in cd.ObjectsAdded)
				{
					var newObj = (ICmObjectSurrogate)m_dbStore.Ext().GetByID(id);
					if (newObj == null)
						continue; // presumably a later transaction deleted it.
					m_dbStore.Ext().Refresh(newObj, 2);
					// There may be some pathological condition in which it got deleted again and we could still
					// answer false, but it is safe for this method to answer true when there is some doubt.
					if (newObj.Classname == classname)
						return true;
				}
			}
			return false;
		}

	}
}
