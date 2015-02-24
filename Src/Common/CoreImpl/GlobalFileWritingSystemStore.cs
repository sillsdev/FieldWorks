using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Collections.Generic;
using SIL.Utils;
using SIL.WritingSystems;

namespace SIL.CoreImpl
{
	/// <summary>
	/// A file-based global writing system store.
	/// </summary>
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification="m_mutex is a singleton; will be disposed by SingletonsContainer")]
	public class GlobalFileWritingSystemStore : IFwWritingSystemStore
	{
		private readonly string m_path;
		/// <summary>Reference to a mutex. The owner of the mutex is the SingletonContainer</summary>
		private readonly Mutex m_mutex;

		/// <summary>
		/// Initializes a new instance of the <see cref="GlobalFileWritingSystemStore"/> class.
		/// </summary>
		public GlobalFileWritingSystemStore()
			: this(DirectoryFinder.GlobalWritingSystemStoreDirectory)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="GlobalFileWritingSystemStore"/> class.
		/// </summary>
		/// <param name="path">The path.</param>
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification="Offending code is not executed on Linux")]
		internal GlobalFileWritingSystemStore(string path)
		{
			m_path = path;
			if (!Directory.Exists(m_path))
			{
				DirectoryInfo di;

				// Provides FW on Linux multi-user access. Overrides the system
				// umask and creates the directory with the permissions "775".
				// The "fieldworks" group was created outside the app during
				// configuration of the package which allows group access.
				using(new FileModeOverride())
				{
					di = Directory.CreateDirectory(m_path);
				}

				if (!MiscUtils.IsUnix)
				{
					// NOTE: GetAccessControl/ModifyAccessRule/SetAccessControl is not implemented in Mono
					DirectorySecurity ds = di.GetAccessControl();
					var sid = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
					AccessRule rule = new FileSystemAccessRule(sid, FileSystemRights.Write | FileSystemRights.ReadAndExecute
						| FileSystemRights.Modify, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
						PropagationFlags.InheritOnly, AccessControlType.Allow);
					bool modified;
					ds.ModifyAccessRule(AccessControlModification.Add, rule, out modified);
					di.SetAccessControl(ds);
				}
			}
			m_mutex = SingletonsContainer.Get(typeof(Mutex).FullName + m_path,
				() => new Mutex(false, m_path.Replace('\\', '_').Replace('/', '_')));
		}

		/// <summary>
		/// Adds the writing system to the store or updates the store information about
		/// an already-existing writing system.  Set should be called when there is a change
		/// that updates the RFC5646 information.
		/// </summary>
		public void Set(WritingSystemDefinition ws)
		{
			m_mutex.WaitOne();
			MemoryStream oldData = null;
			try
			{
				string writingSystemFileName = GetFileName(ws);
				string writingSystemFilePath = GetFilePath(ws);
				if (!ws.IsChanged && File.Exists(writingSystemFilePath))
					return; // no need to save (better to preserve the modified date)
				string oldID = ws.StoreID;
				string incomingFileName = GetFileName(oldID);
				string incomingFilePath = GetFilePath(oldID);
				if (!string.IsNullOrEmpty(incomingFileName))
				{
					if (File.Exists(incomingFilePath))
					{
						// load old data to preserve stuff in LDML that we don't use, but don't throw up an error if it fails
						try
						{
							oldData = new MemoryStream(File.ReadAllBytes(incomingFilePath), false);
						}
						catch
						{
						}
						if (writingSystemFileName != incomingFileName)
						{
							File.Delete(incomingFilePath);
							// JohnT: Added this without fully understanding, to get things to compile. I don't fully
							// know when this event should be raised, nor am I sure I am building the argument correctly.
							// However, I don't think anything (at least in our code) actually uses it.
							if (WritingSystemIDChanged != null)
								WritingSystemIDChanged(this, new WritingSystemIDChangedEventArgs(oldID, ws.ID));
						}
					}
				}
				var ldmlDataMapper = new LdmlDataMapper();
				try
				{
					// Provides FW on Linux multi-user access. Overrides the system
					// umask and creates the files with the permissions "775".
					// The "fieldworks" group was created outside the app during
					// configuration of the package which allows group access.
					using (new FileModeOverride())
					{
						ldmlDataMapper.Write(writingSystemFilePath, ws, oldData);
					}
				}
				catch (UnauthorizedAccessException)
				{
					// If we can't save the changes, too bad. Inability to save locally is typically caught
					// when we go to open the modify dialog. If we can't make the global store consistent,
					// as we well may not be able to in a client-server mode, too bad.
				}

				ws.AcceptChanges();
			}
			finally
			{
				if (oldData != null)
					oldData.Dispose();
				m_mutex.ReleaseMutex();
			}
		}

		/// <summary>
		/// Returns true if a call to Set should succeed, false if a call to Set would throw
		/// </summary>
		public bool CanSet(WritingSystemDefinition ws)
		{
			return true;
		}

		/// <summary>
		/// Gets the writing system object for the given Store ID
		/// </summary>
		public WritingSystemDefinition Get(string identifier)
		{
			m_mutex.WaitOne();
			try
			{
				string filePath = GetFilePath(identifier);
				if (!File.Exists(filePath))
					throw new ArgumentOutOfRangeException("Missing file for writing system code: " + identifier);
				return GetFromFilePath(filePath);
			}
			finally
			{
				m_mutex.ReleaseMutex();
			}
		}

		/// <summary>
		/// If the given writing system were passed to Set, this function returns the
		/// new StoreID that would be assigned.
		/// </summary>
		public string GetNewStoreIDWhenSet(WritingSystemDefinition ws)
		{
			if (ws == null)
				throw new ArgumentNullException("ws");

			return ws.ID;
		}

		/// <summary>
		/// Returns true if a writing system with the given Store ID exists in the store
		/// </summary>
		public bool Contains(string identifier)
		{
			m_mutex.WaitOne();
			try
			{
				return File.Exists(GetFilePath(identifier));
			}
			finally
			{
				m_mutex.ReleaseMutex();
			}
		}

		/// <summary>
		/// Obsolete method retained because required by interface defn for backwards compatibility
		/// </summary>
		/// <param name="identifier"></param>
		/// <returns></returns>
		public bool Exists(string identifier)
		{
			return Contains(identifier);
		}

		/// <summary>
		/// Added to satisfy IWritingSystemRepository definition...implementation copied from WritingSystemRepositoryBase
		/// </summary>
		/// <param name="idsToFilter"></param>
		/// <returns></returns>
		public IEnumerable<string> FilterForTextIDs(IEnumerable<string> idsToFilter)
		{
			return TextWritingSystems.Where(ws => idsToFilter.Contains(ws.ID)).Select(ws => ws.ID);
		}

		/// <summary>
		/// Only needed in local store
		/// </summary>
		/// <param name="layoutName"></param>
		/// <param name="cultureInfo"></param>
		/// <param name="wsCurrent"></param>
		/// <param name="candidates"></param>
		/// <returns></returns>
		public WritingSystemDefinition GetWsForInputLanguage(string layoutName, CultureInfo cultureInfo,
			WritingSystemDefinition wsCurrent, WritingSystemDefinition[] candidates)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Gives the total number of writing systems in the store
		/// </summary>
		public int Count
		{
			get
			{
				m_mutex.WaitOne();
				try
				{
					return Directory.GetFiles(m_path, "*.ldml").Length;
				}
				finally
				{
					m_mutex.ReleaseMutex();
				}
			}
		}

		/// <summary>
		/// Creates a new writing system object and returns it.  Set will need to be called
		/// once identifying information has been changed in order to save it in the store.
		/// </summary>
		public WritingSystemDefinition CreateNew()
		{
			return new WritingSystem();
		}

		/// <summary>
		/// This is a new required interface member. We don't use it, and I hope we don't use anything which uses it!
		/// </summary>
		/// <param name="wsToConflate"></param>
		/// <param name="wsToConflateWith"></param>
		public void Conflate(string wsToConflate, string wsToConflateWith)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Removes the writing system with the specified Store ID from the store.
		/// </summary>
		public void Remove(string identifier)
		{
			m_mutex.WaitOne();
			try
			{
				File.Delete(GetFilePath(identifier));
				if (WritingSystemDeleted != null)
					WritingSystemDeleted(this, new WritingSystemDeletedEventArgs(identifier));
			}
			finally
			{
				m_mutex.ReleaseMutex();
			}
		}

		/// <summary>
		/// Returns a list of all writing system definitions in the store.
		/// </summary>
		public IEnumerable<WritingSystemDefinition> AllWritingSystems
		{
			get
			{
				m_mutex.WaitOne();
				try
				{
					return Directory.GetFiles(m_path, "*.ldml").Select(GetFromFilePath).ToArray();
				}
				finally
				{
					m_mutex.ReleaseMutex();
				}
			}
		}

		/// <summary>
		/// Added to satisfy definition of IWritingSystemRepository...implementation adapted from WritingSystemRepositoryBase
		/// </summary>
		public IEnumerable<WritingSystemDefinition> TextWritingSystems
		{
			get { return AllWritingSystems.Where(ws => !ws.IsVoice); }
		}

		/// <summary>
		/// Added to satisfy definition of IWritingSystemRepository...implementation adapted from WritingSystemRepositoryBase
		/// </summary>
		public IEnumerable<WritingSystemDefinition> VoiceWritingSystems
		{
			get { return AllWritingSystems.Where(ws => ws.IsVoice); }
		}

		/// <summary>
		/// Event raised when writing system ID is changed. Required for interface defn, dubious implementstion.
		/// </summary>
		public event EventHandler<WritingSystemIDChangedEventArgs> WritingSystemIDChanged;
		/// <summary>
		/// Event raised when writing system is deleted. Required for interface defn,  dubious implementstion.
		/// </summary>
		public event EventHandler<WritingSystemDeletedEventArgs> WritingSystemDeleted;

#pragma warning disable 67	// WritingSystemConflated is never used, but part of interface IWritingSystemRepository
		/// <summary/>
		public event EventHandler<WritingSystemConflatedEventArgs> WritingSystemConflated;
#pragma warning restore 67

		/// <summary>
		/// Returns a list of all writing system definitions in the store. (Obsolete)
		/// </summary>
		public IEnumerable<WritingSystemDefinition> WritingSystemDefinitions
		{
			get { return AllWritingSystems; }
		}

		/// <summary>
		/// Makes a duplicate of an existing writing system definition.  Set will need
		/// to be called with this new duplicate once identifying information has been changed
		/// in order to place the new definition in the store.
		/// </summary>
		public WritingSystemDefinition MakeDuplicate(WritingSystemDefinition definition)
		{
			return definition.Clone();
		}

		/// <summary>
		/// This is used by the orphan finder, which we don't use (yet). It tells whether, typically in the scope of some
		/// current change log, a writing system ID has changed to something else...call WritingSystemIdHasChangedTo
		/// to find out what.
		/// </summary>
		public bool WritingSystemIDHasChanged(string id)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// This is used by the orphan finder, which we don't use (yet). It tells what, typically in the scope of some
		/// current change log, a writing system ID has changed to.
		/// </summary>
		public string WritingSystemIDHasChangedTo(string id)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="identifier">The identifier.</param>
		/// <param name="dateModified">The date modified.</param>
		public void LastChecked(string identifier, DateTime dateModified)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Writes the store to a persistable medium, if applicable.
		/// </summary>
		public void Save()
		{
		}

		/// <summary>
		/// Since the current implementation of Save does nothing, it's always possible.
		/// </summary>
		public bool CanSave(WritingSystemDefinition ws, out string path)
		{
			path = "";
			return true;
		}

		/// <summary>
		/// Returns a list of writing systems from rhs which are newer than ones in the store.
		/// </summary>
		// TODO: Maybe this should be IEnumerable<string> .... which returns the identifiers.
		public IEnumerable<WritingSystemDefinition> WritingSystemsNewerIn(IEnumerable<WritingSystemDefinition> rhs)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Added to satisfy definition of IWritingSystemRepository...do we need to do anything?
		/// </summary>
		public void OnWritingSystemIDChange(WritingSystemDefinition ws, string oldId)
		{
		}

		/// <summary>
		/// Gets the specified writing system if it exists.
		/// </summary>
		/// <param name="identifier">The identifier.</param>
		/// <param name="ws">The writing system.</param>
		/// <returns></returns>
		public bool TryGet(string identifier, out WritingSystemDefinition ws)
		{
			m_mutex.WaitOne();
			try
			{
				if (Exists(identifier))
				{
					ws = Get(identifier);
					return true;
				}

				ws = null;
				return false;
			}
			finally
			{
				m_mutex.ReleaseMutex();
			}
		}

		private WritingSystemDefinition GetFromFilePath(string filePath)
		{
			try
			{
				WritingSystemDefinition ws = CreateNew();
				var ldmlDataMapper = new LdmlDataMapper();
				ldmlDataMapper.Read(filePath, ws);
				ws.StoreID = ws.ID;
				ws.AcceptChanges();
				return ws;
			}
			catch (Exception e)
			{
				throw new ArgumentException("GlobalWritingSystemStore was unable to load the LDML file " + filePath, "filePath", e);
			}
		}

		private string GetFilePath(WritingSystemDefinition ws)
		{
			return Path.Combine(m_path, GetFileName(ws));
		}

		private string GetFilePath(string identifier)
		{
			return Path.Combine(m_path, GetFileName(identifier));
		}

		private static string GetFileName(WritingSystemDefinition ws)
		{
			if (string.IsNullOrEmpty(ws.Language))
				return "";
			return GetFileName(ws.ID);
		}

		private static string GetFileName(string identifier)
		{
			if (string.IsNullOrEmpty(identifier))
				return "";
			return identifier + ".ldml";
		}
	}
}
