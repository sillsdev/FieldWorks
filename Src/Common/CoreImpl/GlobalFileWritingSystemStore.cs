using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Xml;
using System.Collections.Generic;
using System.Xml.Linq;
using Palaso.WritingSystems;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;

namespace SIL.CoreImpl
{
	/// <summary>
	/// A file-based global writing system store.
	/// </summary>
	public class GlobalFileWritingSystemStore : IFwWritingSystemStore, IDisposable
	{
		private readonly string m_path;
		private readonly Mutex m_mutex;

		/// <summary>
		/// Initializes a new instance of the <see cref="GlobalFileWritingSystemStore"/> class.
		/// </summary>
		/// <param name="path">The path.</param>
		public GlobalFileWritingSystemStore(string path)
		{
			m_path = path;
			Directory.CreateDirectory(m_path);
			m_mutex = new Mutex(false, m_path.Replace('\\', '_').Replace('/', '_'));
		}

		/// <summary>
		/// Create a clone of the current instance that can/needs to be disposed independently.
		/// </summary>
		public GlobalFileWritingSystemStore Clone()
		{
			return new GlobalFileWritingSystemStore(m_path);
		}

		#region Disposable stuff
		#if DEBUG
		/// <summary/>
		~GlobalFileWritingSystemStore()
		{
			Dispose(false);
		}
		#endif

		/// <summary/>
		public bool IsDisposed
		{
			get;
			private set;
		}

		/// <summary/>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary/>
		protected virtual void Dispose(bool fDisposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (fDisposing && !IsDisposed)
			{
				// dispose managed and unmanaged objects
				var disposable = m_mutex as IDisposable;
				if (disposable != null)
					disposable.Dispose();
			}
			IsDisposed = true;
		}
		#endregion
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
				if (!ws.Modified && File.Exists(writingSystemFilePath))
					return; // no need to save (better to preserve the modified date)
				string incomingFileName = GetFileName(ws.StoreID);
				string incomingFilePath = GetFilePath(ws.StoreID);
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
							File.Delete(incomingFilePath);
					}
				}
				var adaptor = new FwLdmlAdaptor();
				try
				{
					adaptor.Write(writingSystemFilePath, ws, oldData);
				}
				catch (UnauthorizedAccessException)
				{
					// If we can't save the changes, too bad. Inability to save locally is typically caught
					// when we go to open the modify dialog. If we can't make the global store consistent,
					// as we well may not be able to in a client-server mode, too bad.
				}

				ws.Modified = false;
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

			return ws.RFC5646;
		}

		/// <summary>
		/// Returns true if a writing system with the given Store ID exists in the store
		/// </summary>
		public bool Exists(string identifier)
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
			return new PalasoWritingSystem();
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
			}
			finally
			{
				m_mutex.ReleaseMutex();
			}
		}

		/// <summary>
		/// Returns a list of all writing system definitions in the store.
		/// </summary>
		public IEnumerable<WritingSystemDefinition> WritingSystemDefinitions
		{
			get
			{
				m_mutex.WaitOne();
				try
				{
					return Directory.GetFiles(m_path, "*.ldml").Select(filePath => GetFromFilePath(filePath)).ToArray();
				}
				finally
				{
					m_mutex.ReleaseMutex();
				}
			}
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
			WritingSystemDefinition ws = CreateNew();
			var adaptor = new FwLdmlAdaptor();
			adaptor.Read(filePath, ws);
			ws.StoreID = ws.RFC5646;
			ws.Modified = false;
			return ws;
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
			return GetFileName(ws.RFC5646);
		}

		private static string GetFileName(string identifier)
		{
			if (string.IsNullOrEmpty(identifier))
				return "";
			return identifier + ".ldml";
		}
	}
}
