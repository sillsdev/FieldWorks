// Copyright (c) 2013-2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Threading;
using System.Diagnostics.CodeAnalysis;
using ProtoBuf;
using SIL.FieldWorks.FDO.DomainServices.DataMigration;

namespace SIL.FieldWorks.FDO.Infrastructure.Impl
{
	/// <summary>
	/// An XML file-based backend provider that allows multiple applications to access the same project
	/// simultaneously. It uses memory mapped files to maintain a shared commit log that all applications
	/// use to update their state to reflect changes made by other applications. The commit log is
	/// implemented as a circular buffer of commit records. A single peer is responsible for updating
	/// the XML file.
	/// </summary>
	internal class SharedXMLBackendProvider : XMLBackendProvider
	{
		private const int PageSize = 4096;
		private const int CommitLogFileSize = 2500 * PageSize;
		private const int CommitLogMetadataFileSize = 1 * PageSize;

		/// <summary>
		/// This mutex synchronizes access to the commit log, its metadata, and the XML file.
		/// </summary>
		private Mutex m_commitLogMutex;
		private MemoryMappedFile m_commitLogMetadata;
		private MemoryMappedFile m_commitLog;
		private readonly Guid m_peerID;
		private readonly Dictionary<int, Process> m_peerProcesses;

		internal SharedXMLBackendProvider(FdoCache cache, IdentityMap identityMap, ICmObjectSurrogateFactory surrogateFactory, IFwMetaDataCacheManagedInternal mdc,
			IDataMigrationManager dataMigrationManager, IFdoUI ui, IFdoDirectories dirs)
			: base(cache, identityMap, surrogateFactory, mdc, dataMigrationManager, ui, dirs)
		{
			m_peerProcesses = new Dictionary<int, Process>();
			m_peerID = Guid.NewGuid();
		}

		internal int OtherApplicationsConnectedCount
		{
			get
			{
				m_commitLogMutex.WaitOne();
				try
				{
					using (MemoryMappedViewStream stream = m_commitLogMetadata.CreateViewStream())
					{
						CommitLogMetadata metadata = GetMetadata(stream);
						if (CheckExitedPeerProcesses(metadata))
							SaveMetadata(stream, metadata);
						return metadata.Peers.Count - 1;
					}
				}
				finally
				{
					m_commitLogMutex.ReleaseMutex();
				}
			}
		}

		protected override int StartupInternal(int currentModelVersion)
		{
			bool createdNew;
			m_commitLogMutex = new Mutex(true, MutexName, out createdNew);
			if (!createdNew)
				m_commitLogMutex.WaitOne();
			try
			{
				CreateSharedMemory(createdNew);
				using (MemoryMappedViewStream stream = m_commitLogMetadata.CreateViewStream())
				{
					CommitLogMetadata metadata;
					if (!createdNew && TryGetMetadata(stream, out metadata))
						CheckExitedPeerProcesses(metadata);
					else
						metadata = new CommitLogMetadata();

					using (Process curProcess = Process.GetCurrentProcess())
						metadata.Peers[m_peerID] = new CommitLogPeer { ProcessID = curProcess.Id, Generation = metadata.FileGeneration };

					if (metadata.Master == Guid.Empty)
					{
						base.LockProject();
						metadata.Master = m_peerID;
					}

					int startupModelVersion = ReadInSurrogates(currentModelVersion);
					// non-master peers cannot migrate the XML file
					if (startupModelVersion < currentModelVersion && metadata.Master != m_peerID)
						throw new FdoDataMigrationForbiddenException();

					SaveMetadata(stream, metadata);

					return startupModelVersion;
				}
			}
			finally
			{
				m_commitLogMutex.ReleaseMutex();
			}
		}

		protected override void ShutdownInternal()
		{
			if (m_commitLogMutex != null && m_commitLogMetadata != null)
			{
				CompleteAllCommits();
				m_commitLogMutex.WaitOne();
				try
				{
#if __MonoCS__
					bool delete = false;
#endif
					using (MemoryMappedViewStream stream = m_commitLogMetadata.CreateViewStream())
					{
						CommitLogMetadata metadata;
						if (TryGetMetadata(stream, out metadata))
						{
							if (metadata.Master == m_peerID)
							{
								// commit any unseen foreign changes
								List<ICmObjectSurrogate> foreignNewbies;
								List<ICmObjectSurrogate> foreignDirtballs;
								List<ICmObjectId> foreignGoners;
								if (GetUnseenForeignChanges(metadata, out foreignNewbies, out foreignDirtballs, out foreignGoners))
								{
									var newObjects = new HashSet<ICmObjectOrSurrogate>(foreignNewbies);
									var editedObjects = new HashSet<ICmObjectOrSurrogate>(foreignDirtballs);
									var removedObjects = new HashSet<ICmObjectId>(foreignGoners);

									IEnumerable<CustomFieldInfo> fields;
									if (HaveAnythingToCommit(newObjects, editedObjects, removedObjects, out fields) && (StartupVersionNumber == ModelVersion))
										base.WriteCommitWork(new CommitWork(newObjects, editedObjects, removedObjects, fields));
								}
								// XML file is now totally up-to-date
								metadata.FileGeneration = metadata.CurrentGeneration;
							}
							RemovePeer(metadata, m_peerID);
#if __MonoCS__
							delete = metadata.Peers.Count == 0;
#endif
							SaveMetadata(stream, metadata);
						}
					}

					base.UnlockProject();

					m_commitLog.Dispose();
					m_commitLog = null;

					m_commitLogMetadata.Dispose();
					m_commitLogMetadata = null;

#if __MonoCS__
					if (delete)
					{
						File.Delete(Path.Combine(Path.GetTempPath(), CommitLogMetadataName));
						File.Delete(Path.Combine(Path.GetTempPath(), CommitLogName));
					}
#endif
				}
				finally
				{
					m_commitLogMutex.ReleaseMutex();
				}
			}

			if (m_commitLogMutex != null)
			{
				m_commitLogMutex.Dispose();
				m_commitLogMutex = null;
			}

			if (CommitThread != null)
			{
				CommitThread.Stop();
				CommitThread.Dispose();
				CommitThread = null;
			}

			foreach (Process peerProcess in m_peerProcesses.Values)
				peerProcess.Close();
			m_peerProcesses.Clear();
		}

		protected override void CreateInternal()
		{
			bool createdNew;
			m_commitLogMutex = new Mutex(true, MutexName, out createdNew);
			if (!createdNew)
				throw new InvalidOperationException("Cannot create shared XML backend.");
			try
			{
				CreateSharedMemory(true);
				var metadata = new CommitLogMetadata { Master = m_peerID };
				using (Process curProcess = Process.GetCurrentProcess())
					metadata.Peers[m_peerID] = new CommitLogPeer { ProcessID = curProcess.Id, Generation = metadata.FileGeneration };
				using (MemoryMappedViewStream stream = m_commitLogMetadata.CreateViewStream())
				{
					SaveMetadata(stream, metadata);
				}

				base.CreateInternal();
			}
			finally
			{
				m_commitLogMutex.ReleaseMutex();
			}
		}

		private static bool TryGetMetadata(MemoryMappedViewStream stream, out CommitLogMetadata metadata)
		{
			stream.Seek(0, SeekOrigin.Begin);
			int length;
			if (Serializer.TryReadLengthPrefix(stream, PrefixStyle.Base128, out length) && length > 0)
			{
				stream.Seek(0, SeekOrigin.Begin);
				metadata = Serializer.DeserializeWithLengthPrefix<CommitLogMetadata>(stream, PrefixStyle.Base128, 1);
				return true;
			}

			metadata = null;
			return false;
		}

		private static CommitLogMetadata GetMetadata(MemoryMappedViewStream stream)
		{
			stream.Seek(0, SeekOrigin.Begin);
			return Serializer.DeserializeWithLengthPrefix<CommitLogMetadata>(stream, PrefixStyle.Base128, 1);
		}

		private static void SaveMetadata(MemoryMappedViewStream stream, CommitLogMetadata metadata)
		{
			stream.Seek(0, SeekOrigin.Begin);
			Serializer.SerializeWithLengthPrefix(stream, metadata, PrefixStyle.Base128, 1);
		}

		private string MutexName
		{
			get { return ProjectId.Name + "_Mutex"; }
		}

		private string CommitLogName
		{
			get { return ProjectId.Name + "_CommitLog"; }
		}

		private string CommitLogMetadataName
		{
			get { return ProjectId.Name + "_CommitLogMetadata"; }
		}

		private void CreateSharedMemory(bool createdNew)
		{
			m_commitLogMetadata = CreateOrOpen(CommitLogMetadataName, CommitLogMetadataFileSize, createdNew);
			m_commitLog = CreateOrOpen(CommitLogName, CommitLogFileSize, createdNew);
		}

		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification = "An actual file is passed into MemoryMappedFile.CreateOrOpen")]
		private MemoryMappedFile CreateOrOpen(string name, long capacity, bool createdNew)
		{
#if __MonoCS__
			name = Path.Combine(Path.GetTempPath(), name);
			// delete old file that could be left after a crash
			if (createdNew && File.Exists(name))
				File.Delete(name);

			// Mono only supports memory mapped files that are backed by an actual file
			if (!File.Exists(name))
			{
				using (var fs = new FileStream(name, FileMode.CreateNew))
					fs.SetLength(capacity);
			}
#endif
			return MemoryMappedFile.CreateOrOpen(name, capacity);
		}

		/// <summary>
		/// Checks for peer processes that have exited unexpectedly and update the metadata accordingly.
		/// </summary>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "The process is disposed later.")]
		private bool CheckExitedPeerProcesses(CommitLogMetadata metadata)
		{
			bool changed = false;
			var processesToRemove = new HashSet<Process>(m_peerProcesses.Values);
			foreach (KeyValuePair<Guid, CommitLogPeer> kvp in metadata.Peers.ToArray())
			{
				if (kvp.Key == m_peerID)
					continue;

				Process process;
				if (m_peerProcesses.TryGetValue(kvp.Value.ProcessID, out process))
				{
					if (process.HasExited)
					{
						RemovePeer(metadata, kvp.Key);
						changed = true;
					}
					else
					{
						processesToRemove.Remove(process);
					}
				}
				else
				{
					try
					{
						process = Process.GetProcessById(kvp.Value.ProcessID);
						m_peerProcesses[kvp.Value.ProcessID] = process;
					}
					catch (ArgumentException)
					{
						RemovePeer(metadata, kvp.Key);
						changed = true;
					}
				}
			}

			foreach (Process process in processesToRemove)
			{
				m_peerProcesses.Remove(process.Id);
				process.Close();
			}
			return changed;
		}

		private static void RemovePeer(CommitLogMetadata metadata, Guid peerID)
		{
			metadata.Peers.Remove(peerID);
			if (metadata.Master == peerID)
				metadata.Master = Guid.Empty;
		}

		internal override void LockProject()
		{
			m_commitLogMutex.WaitOne();
			try
			{
				base.LockProject();
				using (MemoryMappedViewStream stream = m_commitLogMetadata.CreateViewStream())
				{
					CommitLogMetadata metadata = GetMetadata(stream);
					if (metadata.Master == Guid.Empty)
					{
						metadata.Master = m_peerID;
						SaveMetadata(stream, metadata);
					}
				}
			}
			finally
			{
				m_commitLogMutex.ReleaseMutex();
			}
		}

		internal override void UnlockProject()
		{
			m_commitLogMutex.WaitOne();
			try
			{
				base.UnlockProject();
				using (MemoryMappedViewStream stream = m_commitLogMetadata.CreateViewStream())
				{
					CommitLogMetadata metadata;
					if (TryGetMetadata(stream, out metadata))
					{
						if (metadata.Master == m_peerID)
						{
							metadata.Master = Guid.Empty;
							SaveMetadata(stream, metadata);
						}
					}
				}
			}
			finally
			{
				m_commitLogMutex.ReleaseMutex();
			}
		}

		public override bool Commit(HashSet<ICmObjectOrSurrogate> newbies, HashSet<ICmObjectOrSurrogate> dirtballs, HashSet<ICmObjectId> goners)
		{
			CommitLogMetadata metadata = null;
			m_commitLogMutex.WaitOne();
			try
			{
				using (MemoryMappedViewStream stream = m_commitLogMetadata.CreateViewStream())
				{
					metadata = GetMetadata(stream);
				}

				List<ICmObjectSurrogate> foreignNewbies;
				List<ICmObjectSurrogate> foreignDirtballs;
				List<ICmObjectId> foreignGoners;
				if (GetUnseenForeignChanges(metadata, out foreignNewbies, out foreignDirtballs, out foreignGoners))
				{
					// we have now seen every commit generation
					metadata.Peers[m_peerID].Generation = metadata.CurrentGeneration;

					IUnitOfWorkService uowService = ((IServiceLocatorInternal) m_cache.ServiceLocator).UnitOfWorkService;
					IReconcileChanges reconciler = uowService.CreateReconciler(foreignNewbies, foreignDirtballs, foreignGoners);
					if (reconciler.OkToReconcileChanges())
					{
						reconciler.ReconcileForeignChanges();
						if (metadata.Master == m_peerID)
						{
							var newObjects = new HashSet<ICmObjectOrSurrogate>(foreignNewbies);
							var editedObjects = new HashSet<ICmObjectOrSurrogate>(foreignDirtballs);
							var removedObjects = new HashSet<ICmObjectId>(foreignGoners);

							IEnumerable<CustomFieldInfo> fields;
							if (HaveAnythingToCommit(newObjects, editedObjects, removedObjects, out fields) && (StartupVersionNumber == ModelVersion))
								PerformCommit(newObjects, editedObjects, removedObjects, fields);
						}
					}
					else
					{
						uowService.ConflictingChanges(reconciler);
						return true;
					}
				}

				CheckExitedPeerProcesses(metadata);
				if (metadata.Master == Guid.Empty)
				{
					// Check if the former master left the commit log and XML file in a consistent state. If not, we can't continue.
					if (metadata.CurrentGeneration != metadata.FileGeneration)
						return false;
					base.LockProject();
					metadata.Master = m_peerID;
				}

				IEnumerable<CustomFieldInfo> cfiList;
				if (!HaveAnythingToCommit(newbies, dirtballs, goners, out cfiList) && (StartupVersionNumber == ModelVersion))
					return true;

				metadata.CurrentGeneration++;

				var commitRec = new CommitLogRecord
					{
						Source = m_peerID,
						WriteGeneration = metadata.CurrentGeneration,
						ObjectsDeleted = goners.Select(g => g.Guid).ToList(),
						ObjectsAdded = newbies.Select(n => n.XMLBytes).ToList(),
						ObjectsUpdated = dirtballs.Select(d => d.XMLBytes).ToList()
					};

				// we've seen our own change
				metadata.Peers[m_peerID].Generation = metadata.CurrentGeneration;

				using (var buffer = new MemoryStream())
				{
					Serializer.SerializeWithLengthPrefix(buffer, commitRec, PrefixStyle.Base128, 1);
					if (metadata.LogLength + buffer.Length > CommitLogFileSize)
						return false;

					byte[] bytes = buffer.GetBuffer();
					int commitRecOffset = (metadata.LogOffset + metadata.LogLength) % CommitLogFileSize;
					// check if the record can fit at the end of the commit log. If not, we wrap around to the beginning.
					if (commitRecOffset + buffer.Length > CommitLogFileSize)
					{
						metadata.Padding = CommitLogFileSize - commitRecOffset;
						metadata.LogLength += metadata.Padding;
						commitRecOffset = 0;
					}
					using (MemoryMappedViewStream stream = m_commitLog.CreateViewStream(commitRecOffset, buffer.Length))
					{
						stream.Write(bytes, 0, (int) buffer.Length);
						metadata.LogLength += (int) buffer.Length;
					}
				}

				if (metadata.Master == m_peerID)
					PerformCommit(newbies, dirtballs, goners, cfiList);

				return true;
			}
			finally
			{
				try
				{
					if (metadata != null)
					{
						using (MemoryMappedViewStream stream = m_commitLogMetadata.CreateViewStream())
						{
							SaveMetadata(stream, metadata);
						}
					}
				}
				finally
				{
					m_commitLogMutex.ReleaseMutex();
				}
			}
		}

		protected override void WriteCommitWork(CommitWork workItem)
		{
			m_commitLogMutex.WaitOne();
			try
			{
				base.WriteCommitWork(workItem);

				using (MemoryMappedViewStream stream = m_commitLogMetadata.CreateViewStream())
				{
					CommitLogMetadata metadata = GetMetadata(stream);
					metadata.FileGeneration = metadata.Peers[m_peerID].Generation;
					SaveMetadata(stream, metadata);
				}
			}
			finally
			{
				m_commitLogMutex.ReleaseMutex();
			}
		}

		/// <summary>
		/// Gets all unseen foreign changes from the commit log. The metadata should be saved after calling this method,
		/// because inactive records might have been purged.
		/// </summary>
		private bool GetUnseenForeignChanges(CommitLogMetadata metadata,
			out List<ICmObjectSurrogate> foreignNewbies,
			out List<ICmObjectSurrogate> foreignDirtballs,
			out List<ICmObjectId> foreignGoners)
		{
			foreignNewbies = new List<ICmObjectSurrogate>();
			foreignDirtballs = new List<ICmObjectSurrogate>();
			foreignGoners = new List<ICmObjectId>();

			int minPeerGeneration = metadata.Peers.Select(p => p.Key == m_peerID ? metadata.CurrentGeneration : p.Value.Generation).Min();
			var unseenCommitRecs = new List<CommitLogRecord>();

			int bytesRemaining = metadata.LogLength;
			// read all records up to the end of the file or the end of the log, whichever comes first
			int length = Math.Min(metadata.LogLength, CommitLogFileSize - metadata.LogOffset - metadata.Padding);
			bytesRemaining -= ReadUnseenCommitRecords(metadata, minPeerGeneration, metadata.LogOffset, length, unseenCommitRecs);
			// if there are bytes remaining, it means that we hit the end of the file, so we need to wrap around to the beginning
			if (bytesRemaining > 0)
				bytesRemaining -= ReadUnseenCommitRecords(metadata, minPeerGeneration, 0, bytesRemaining, unseenCommitRecs);
			Debug.Assert(bytesRemaining == 0);

			if (unseenCommitRecs.Count == 0)
				return false;

			var idFactory = m_cache.ServiceLocator.GetInstance<ICmObjectIdFactory>();

			var newbies = new Dictionary<Guid, ICmObjectSurrogate>();
			var dirtballs = new Dictionary<Guid, ICmObjectSurrogate>();
			var goners = new HashSet<Guid>();

			var surrogateFactory = m_cache.ServiceLocator.GetInstance<ICmObjectSurrogateFactory>();

			foreach (CommitLogRecord commitRec in unseenCommitRecs)
			{
				if (commitRec.ObjectsDeleted != null)
				{
					foreach (Guid goner in commitRec.ObjectsDeleted)
					{
						// If it was created by a previous foreign change we haven't seen, we can just forget it.
						if (newbies.Remove(goner))
							continue;
						// If it was modified by a previous foreign change we haven't seen, we can forget the modification.
						// (but we still need to know it's gone).
						dirtballs.Remove(goner);
						goners.Add(goner);
					}
				}
				if (commitRec.ObjectsUpdated != null)
				{
					foreach (byte[] dirtballXml in commitRec.ObjectsUpdated)
					{
						ICmObjectSurrogate dirtballSurrogate = surrogateFactory.Create(DataSortingService.Utf8.GetString(dirtballXml));
						// This shouldn't be necessary; if a previous foreign transaction deleted it, it
						// should not show up as a dirtball in a later transaction until it has shown up as a newby.
						// goners.Remove(dirtball);
						// If this was previously known as a newby or modified, then to us it still is.
						if (newbies.ContainsKey(dirtballSurrogate.Guid) || dirtballs.ContainsKey(dirtballSurrogate.Guid))
							continue;
						dirtballs[dirtballSurrogate.Guid] = dirtballSurrogate;
					}
				}
				if (commitRec.ObjectsAdded != null)
				{
					foreach (byte[] newbyXml in commitRec.ObjectsAdded)
					{
						ICmObjectSurrogate newObj = surrogateFactory.Create(DataSortingService.Utf8.GetString(newbyXml));
						if (goners.Remove(newObj.Guid))
						{
							// an object which an earlier transaction deleted is being re-created.
							// This means that to us, it is a dirtball.
							dirtballs[newObj.Guid] = newObj;
							continue;
						}
						// It shouldn't be in dirtballs; can't be new in one transaction without having been deleted previously.
						// So it really is new.
						newbies[newObj.Guid] = newObj;
					}
				}
				foreignNewbies.AddRange(newbies.Values);
				foreignDirtballs.AddRange(dirtballs.Values);
				foreignGoners.AddRange(from guid in goners select idFactory.FromGuid(guid));
			}
			return true;
		}

		private int ReadUnseenCommitRecords(CommitLogMetadata metadata, int minPeerGeneration, int startOffset, int length, List<CommitLogRecord> unseenCommits)
		{
			if (length == 0)
				return 0;

			int generation = metadata.Peers[m_peerID].Generation;
			using (MemoryMappedViewStream stream = m_commitLog.CreateViewStream(startOffset, length))
			{
				while (stream.Position < length)
				{
					long startPos = stream.Position;
					var rec = Serializer.DeserializeWithLengthPrefix<CommitLogRecord>(stream, PrefixStyle.Base128, 1);
					if (rec.WriteGeneration > generation && rec.Source != m_peerID)
						unseenCommits.Add(rec);
					// remove the record from the commit log once all peers have seen it and it has been written to disk
					if (rec.WriteGeneration <= minPeerGeneration && rec.WriteGeneration <= metadata.FileGeneration)
					{
						metadata.LogOffset = startOffset + (int) stream.Position;
						metadata.LogLength -= (int) (stream.Position - startPos);
					}
				}
			}

			// check if we've purged all records up to the end of the file. If so, wrap around to the beginning.
			if (metadata.LogOffset == CommitLogFileSize - metadata.Padding)
			{
				metadata.LogOffset = 0;
				metadata.LogLength -= metadata.Padding;
				length += metadata.Padding;
				metadata.Padding = 0;
			}

			return length;
		}

		public override bool RenameDatabase(string sNewProjectName)
		{
			if (OtherApplicationsConnectedCount > 0)
				return false;
			return base.RenameDatabase(sNewProjectName);
		}
	}
}
