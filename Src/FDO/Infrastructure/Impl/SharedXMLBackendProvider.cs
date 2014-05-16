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
	internal class SharedXMLBackendProvider : XMLBackendProvider
	{
		private const int PageSize = 4096;
		private const int CommitLogSize = 2500 * PageSize;
		private const int CommitLogMetadataSize = 1 * PageSize;

		private Mutex m_mutex;
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
				m_mutex.WaitOne();
				try
				{
					using (MemoryMappedViewStream stream = m_commitLogMetadata.CreateViewStream())
					{
						var metadata = Serializer.DeserializeWithLengthPrefix<CommitLogMetadata>(stream, PrefixStyle.Base128, 1);
						if (CheckExitedPeerProcesses(metadata))
						{
							stream.Seek(0, SeekOrigin.Begin);
							Serializer.SerializeWithLengthPrefix(stream, metadata, PrefixStyle.Base128, 1);
						}
						return metadata.Peers.Count - 1;
					}
				}
				finally
				{
					m_mutex.ReleaseMutex();
				}
			}
		}

		protected override int StartupInternal(int currentModelVersion)
		{
			bool createdNew;
			m_mutex = new Mutex(true, MutexName, out createdNew);
			if (!createdNew)
				m_mutex.WaitOne();
			try
			{
				CreateSharedMemory(createdNew);
				using (MemoryMappedViewStream stream = m_commitLogMetadata.CreateViewStream())
				{
					CommitLogMetadata metadata;
					int length;
					if (!createdNew && Serializer.TryReadLengthPrefix(stream, PrefixStyle.Base128, out length) && length > 0)
					{
						stream.Seek(0, SeekOrigin.Begin);
						metadata = Serializer.DeserializeWithLengthPrefix<CommitLogMetadata>(stream, PrefixStyle.Base128, 1);
						CheckExitedPeerProcesses(metadata);
					}
					else
					{
						metadata = CreateEmptyMetadata();
					}

					using (Process curProcess = Process.GetCurrentProcess())
						metadata.Peers[m_peerID] = new CommitLogPeer { ProcessID = curProcess.Id, Generation = metadata.FileGeneration };

					if (metadata.Master == Guid.Empty)
					{
						base.LockProject();
						metadata.Master = m_peerID;
					}

					stream.Seek(0, SeekOrigin.Begin);
					Serializer.SerializeWithLengthPrefix(stream, metadata, PrefixStyle.Base128, 1);
				}

				int startupModelVersion = ReadInSurrogates(currentModelVersion);
				if (startupModelVersion < currentModelVersion && !HasLockFile)
					throw new FdoDataMigrationForbiddenException();
				return startupModelVersion;
			}
			finally
			{
				m_mutex.ReleaseMutex();
			}
		}

		protected override void ShutdownInternal()
		{
			if (m_mutex != null && m_commitLogMetadata != null)
			{
				CompleteAllCommits();
				m_mutex.WaitOne();
				try
				{
#if __MonoCS__
					bool delete = false;
#endif
					using (MemoryMappedViewStream stream = m_commitLogMetadata.CreateViewStream())
					{
						int length;
						if (Serializer.TryReadLengthPrefix(stream, PrefixStyle.Base128, out length) && length > 0)
						{
							stream.Seek(0, SeekOrigin.Begin);
							var metadata = Serializer.DeserializeWithLengthPrefix<CommitLogMetadata>(stream, PrefixStyle.Base128, 1);

							if (HasLockFile)
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
							stream.Seek(0, SeekOrigin.Begin);
							Serializer.SerializeWithLengthPrefix(stream, metadata, PrefixStyle.Base128, 1);
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
					m_mutex.ReleaseMutex();
				}
			}

			if (m_mutex != null)
			{
				m_mutex.Dispose();
				m_mutex = null;
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
			m_mutex = new Mutex(true, MutexName, out createdNew);
			if (!createdNew)
				throw new InvalidOperationException("Cannot create shared XML backend.");
			try
			{
				CreateSharedMemory(true);
				CommitLogMetadata metadata = CreateEmptyMetadata();
				metadata.Master = m_peerID;
				using (Process curProcess = Process.GetCurrentProcess())
					metadata.Peers[m_peerID] = new CommitLogPeer { ProcessID = curProcess.Id, Generation = metadata.FileGeneration };
				using (MemoryMappedViewStream stream = m_commitLogMetadata.CreateViewStream())
				{
					Serializer.SerializeWithLengthPrefix(stream, metadata, PrefixStyle.Base128, 1);
				}

				base.CreateInternal();
			}
			finally
			{
				m_mutex.ReleaseMutex();
			}
		}

		private static CommitLogMetadata CreateEmptyMetadata()
		{
			return new CommitLogMetadata { Peers = new Dictionary<Guid, CommitLogPeer>() };
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
			m_commitLogMetadata = CreateOrOpen(CommitLogMetadataName, CommitLogMetadataSize, createdNew);
			m_commitLog = CreateOrOpen(CommitLogName, CommitLogSize, createdNew);
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
			m_mutex.WaitOne();
			try
			{
				base.LockProject();
				using (MemoryMappedViewStream stream = m_commitLogMetadata.CreateViewStream())
				{
					var metadata = Serializer.DeserializeWithLengthPrefix<CommitLogMetadata>(stream, PrefixStyle.Base128, 1);
					if (metadata.Master == Guid.Empty)
					{
						metadata.Master = m_peerID;
						stream.Seek(0, SeekOrigin.Begin);
						Serializer.SerializeWithLengthPrefix(stream, metadata, PrefixStyle.Base128, 1);
					}
				}
			}
			finally
			{
				m_mutex.ReleaseMutex();
			}
		}

		internal override void UnlockProject()
		{
			m_mutex.WaitOne();
			try
			{
				base.UnlockProject();
				using (MemoryMappedViewStream stream = m_commitLogMetadata.CreateViewStream())
				{
					int length;
					if (Serializer.TryReadLengthPrefix(stream, PrefixStyle.Base128, out length) && length > 0)
					{
						stream.Seek(0, SeekOrigin.Begin);
						var metadata = Serializer.DeserializeWithLengthPrefix<CommitLogMetadata>(stream, PrefixStyle.Base128, 1);
						if (metadata.Master == m_peerID)
						{
							metadata.Master = Guid.Empty;
							stream.Seek(0, SeekOrigin.Begin);
							Serializer.SerializeWithLengthPrefix(stream, metadata, PrefixStyle.Base128, 1);
						}
					}
				}
			}
			finally
			{
				m_mutex.ReleaseMutex();
			}
		}

		public override bool Commit(HashSet<ICmObjectOrSurrogate> newbies, HashSet<ICmObjectOrSurrogate> dirtballs, HashSet<ICmObjectId> goners)
		{
			CommitLogMetadata metadata = null;
			m_mutex.WaitOne();
			try
			{
				using (MemoryMappedViewStream stream = m_commitLogMetadata.CreateViewStream())
				{
					metadata = Serializer.DeserializeWithLengthPrefix<CommitLogMetadata>(stream, PrefixStyle.Base128, 1);
				}

				List<ICmObjectSurrogate> foreignNewbies;
				List<ICmObjectSurrogate> foreignDirtballs;
				List<ICmObjectId> foreignGoners;
				if (GetUnseenForeignChanges(metadata, out foreignNewbies, out foreignDirtballs, out foreignGoners))
				{
					IUnitOfWorkService uowService = ((IServiceLocatorInternal) m_cache.ServiceLocator).UnitOfWorkService;
					IReconcileChanges reconciler = uowService.CreateReconciler(foreignNewbies, foreignDirtballs, foreignGoners);
					if (reconciler.OkToReconcileChanges())
					{
						reconciler.ReconcileForeignChanges();
						if (HasLockFile)
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
					if (metadata.Length + buffer.Length > CommitLogSize)
						return false;

					byte[] bytes = buffer.GetBuffer();
					int offset = (metadata.Offset + metadata.Length) % CommitLogSize;
					// check if the record can fit at the end of the commit log. If not, we wrap around to the beginning.
					if (offset + buffer.Length > CommitLogSize)
					{
						metadata.Padding = CommitLogSize - offset;
						metadata.Length += metadata.Padding;
						offset = 0;
					}
					using (MemoryMappedViewStream stream = m_commitLog.CreateViewStream(offset, buffer.Length))
					{
						stream.Write(bytes, 0, (int) buffer.Length);
						metadata.Length += (int) buffer.Length;
					}
				}

				if (HasLockFile)
					PerformCommit(newbies, dirtballs, goners, cfiList);

				return true;
			}
			finally
			{
				if (metadata != null)
				{
					using (MemoryMappedViewStream stream = m_commitLogMetadata.CreateViewStream())
					{
						Serializer.SerializeWithLengthPrefix(stream, metadata, PrefixStyle.Base128, 1);
					}
				}
				m_mutex.ReleaseMutex();
			}
		}

		protected override void WriteCommitWork(CommitWork workItem)
		{
			m_mutex.WaitOne();
			try
			{
				base.WriteCommitWork(workItem);

				using (MemoryMappedViewStream stream = m_commitLogMetadata.CreateViewStream())
				{
					var metadata = Serializer.DeserializeWithLengthPrefix<CommitLogMetadata>(stream, PrefixStyle.Base128, 1);
					metadata.FileGeneration = metadata.Peers[m_peerID].Generation;
					stream.Seek(0, SeekOrigin.Begin);
					Serializer.SerializeWithLengthPrefix(stream, metadata, PrefixStyle.Base128, 1);
				}
			}
			finally
			{
				m_mutex.ReleaseMutex();
			}
		}

		private bool GetUnseenForeignChanges(CommitLogMetadata metadata,
			out List<ICmObjectSurrogate> foreignNewbies,
			out List<ICmObjectSurrogate> foreignDirtballs,
			out List<ICmObjectId> foreignGoners)
		{
			foreignNewbies = new List<ICmObjectSurrogate>();
			foreignDirtballs = new List<ICmObjectSurrogate>();
			foreignGoners = new List<ICmObjectId>();

			int startGeneration = metadata.Peers[m_peerID].Generation;
			metadata.Peers[m_peerID].Generation = metadata.CurrentGeneration;
			int minPeerGeneration = metadata.Peers.Values.Min(s => s.Generation);
			var unseenCommits = new List<CommitLogRecord>();

			int viewOffset = metadata.Offset;
			int origLength = metadata.Length;
			int curLength = 0;
			int viewLength = Math.Min(origLength, CommitLogSize - metadata.Offset - metadata.Padding);
			while (curLength < origLength)
			{
				if (viewLength > 0)
				{
					using (MemoryMappedViewStream stream = m_commitLog.CreateViewStream(viewOffset, viewLength))
					{
						while (stream.Position < viewLength)
						{
							var rec = Serializer.DeserializeWithLengthPrefix<CommitLogRecord>(stream, PrefixStyle.Base128, 1);
							if (rec.WriteGeneration > startGeneration && rec.Source != m_peerID)
								unseenCommits.Add(rec);
							// remove the record from the commit log once all peers have seen it and it has been written to disk
							if (rec.WriteGeneration <= minPeerGeneration && rec.WriteGeneration <= metadata.FileGeneration)
								metadata.Offset = viewOffset + (int) stream.Position;
						}
					}
				}
				curLength += viewLength;
				metadata.Length -= metadata.Offset - viewOffset;
				// check if we've hit the end of the commit log. If so, wrap around to the beginning.
				if (metadata.Offset == CommitLogSize - metadata.Padding)
				{
					metadata.Length -= metadata.Padding;
					curLength += metadata.Padding;
					metadata.Padding = 0;
				}
				viewOffset = 0;
				viewLength = origLength - curLength;
			}

			if (unseenCommits.Count == 0)
				return false;

			var idFactory = m_cache.ServiceLocator.GetInstance<ICmObjectIdFactory>();

			var newbies = new Dictionary<Guid, ICmObjectSurrogate>();
			var dirtballs = new Dictionary<Guid, ICmObjectSurrogate>();
			var goners = new HashSet<Guid>();

			var surrogateFactory = m_cache.ServiceLocator.GetInstance<ICmObjectSurrogateFactory>();

			foreach (CommitLogRecord commitRec in unseenCommits)
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
						// We already have its CURRENT data from the object itself.
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

		public override bool RenameDatabase(string sNewProjectName)
		{
			if (OtherApplicationsConnectedCount > 0)
				return false;
			return base.RenameDatabase(sNewProjectName);
		}
	}
}
