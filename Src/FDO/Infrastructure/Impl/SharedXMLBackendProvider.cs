// Copyright (c) 2013-2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Threading;
using ProtoBuf;
using SIL.FieldWorks.FDO.DomainServices.DataMigration;

namespace SIL.FieldWorks.FDO.Infrastructure.Impl
{
	internal class SharedXMLBackendProvider : XMLBackendProvider
	{
		private const int CommitLogSize = 10000 * 4096;

		private Mutex m_mutex;
		private MemoryMappedFile m_commitLogMetadata;
		private MemoryMappedFile m_commitLog;
		private int m_slotIndex = -1;

		internal SharedXMLBackendProvider(FdoCache cache, IdentityMap identityMap, ICmObjectSurrogateFactory surrogateFactory, IFwMetaDataCacheManagedInternal mdc,
			IDataMigrationManager dataMigrationManager, IFdoUI ui, IFdoDirectories dirs)
			: base(cache, identityMap, surrogateFactory, mdc, dataMigrationManager, ui, dirs)
		{
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
						return metadata.Slots.Count(s => s != -1) - 1;
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
			BasicInit();
			m_mutex.WaitOne();
			try
			{
				using (MemoryMappedViewStream stream = m_commitLogMetadata.CreateViewStream())
				{
					CommitLogMetadata metadata;
					int length;
					if (Serializer.TryReadLengthPrefix(stream, PrefixStyle.Base128, out length) && length > 0)
					{
						stream.Seek(0, SeekOrigin.Begin);
						metadata = Serializer.DeserializeWithLengthPrefix<CommitLogMetadata>(stream, PrefixStyle.Base128, 1);
					}
					else
					{
						metadata = new CommitLogMetadata { Slots = new int[8], Master = -1 };
						for (int i = 0; i < metadata.Slots.Length; i++)
							metadata.Slots[i] = -1;
					}

					for (int i = 0; i < metadata.Slots.Length; i++)
					{
						if (metadata.Slots[i] == -1)
						{
							m_slotIndex = i;
							metadata.Slots[i] = metadata.FileGeneration;
							break;
						}
					}

					if (metadata.Master == -1)
					{
						base.LockProject();
						metadata.Master = m_slotIndex;
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
					using (MemoryMappedViewStream stream = m_commitLogMetadata.CreateViewStream())
					{
						int length;
						if (Serializer.TryReadLengthPrefix(stream, PrefixStyle.Base128, out length) && length > 0)
						{
							stream.Seek(0, SeekOrigin.Begin);
							var metadata = Serializer.DeserializeWithLengthPrefix<CommitLogMetadata>(stream, PrefixStyle.Base128, 1);
							metadata.Slots[m_slotIndex] = -1;
							if (metadata.Master == m_slotIndex)
								metadata.Master = -1;
							stream.Seek(0, SeekOrigin.Begin);
							Serializer.SerializeWithLengthPrefix(stream, metadata, PrefixStyle.Base128, 1);
						}
					}

					base.UnlockProject();
				}
				finally
				{
					m_mutex.ReleaseMutex();
				}
			}

			base.ShutdownInternal();

			if (m_commitLog != null)
			{
				m_commitLog.Dispose();
				m_commitLog = null;
			}
			if (m_commitLogMetadata != null)
			{
				m_commitLogMetadata.Dispose();
				m_commitLogMetadata = null;
			}
			if (m_mutex != null)
			{
				m_mutex.Dispose();
				m_mutex = null;
			}
		}

		protected override void CreateInternal()
		{
			BasicInit();
			m_mutex.WaitOne();
			try
			{
				var metadata = new CommitLogMetadata { Slots = new int[8] };
				m_slotIndex = 0;
				metadata.Master = 0;
				metadata.Slots[0] = metadata.FileGeneration;
				for (int i = 1; i < metadata.Slots.Length; i++)
					metadata.Slots[i] = -1;
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

		private void BasicInit()
		{
			m_mutex = new Mutex(false, ProjectId.Name + "_Mutex");
			m_commitLogMetadata = MemoryMappedFile.CreateOrOpen(ProjectId.Name + "_CommitLogMetadata", 1024);
			m_commitLog = MemoryMappedFile.CreateOrOpen(ProjectId.Name + "_CommitLog", CommitLogSize);
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
					if (metadata.Master == -1)
					{
						metadata.Master = m_slotIndex;
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
						if (metadata.Master == m_slotIndex)
						{
							metadata.Master = -1;
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

				if (metadata.Master == -1)
				{
					base.LockProject();
					metadata.Master = m_slotIndex;
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

				IEnumerable<CustomFieldInfo> cfiList;
				if (!HaveAnythingToCommit(newbies, dirtballs, goners, out cfiList) && (StartupVersionNumber == ModelVersion))
					return true;

				metadata.CurrentGeneration++;

				var commitRec = new CommitLogRecord
					{
						Source = m_slotIndex,
						WriteGeneration = metadata.CurrentGeneration,
						ObjectsDeleted = goners.Select(g => g.Guid).ToList(),
						ObjectsAdded = newbies.Select(n => n.XMLBytes).ToList(),
						ObjectsUpdated = dirtballs.Select(d => d.XMLBytes).ToList()
					};

				// we've seen our own change, and we use a semaphore to make sure there haven't been others since we checked.
				metadata.Slots[m_slotIndex] = metadata.CurrentGeneration;

				// TODO handle custom fields

				using (var buffer = new MemoryStream())
				{
					Serializer.SerializeWithLengthPrefix(buffer, commitRec, PrefixStyle.Base128, 1);

					if (metadata.Length + buffer.Length > CommitLogSize)
						return false;

					byte[] bytes = buffer.GetBuffer();
					int offset = (metadata.Offset + metadata.Length) % CommitLogSize;
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
					metadata.FileGeneration = metadata.Slots[m_slotIndex];
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

			int startGeneration = metadata.Slots[m_slotIndex];
			metadata.Slots[m_slotIndex] = metadata.CurrentGeneration;
			int minGeneration = metadata.Slots.Where(g => g != -1).Min();
			var unseenCommits = new List<CommitLogRecord>();

			int viewOffset = metadata.Offset;
			int origLength = metadata.Length;
			int curLength = 0;
			int viewLength = Math.Min(origLength, CommitLogSize - metadata.Offset - metadata.Padding);
			while (curLength < origLength)
			{
				if (viewLength > 0)
				{
					using (MemoryMappedViewStream stream = m_commitLog.CreateViewStream(viewOffset, viewLength, MemoryMappedFileAccess.Read))
					{
						while (stream.Position < viewLength)
						{
							var rec = Serializer.DeserializeWithLengthPrefix<CommitLogRecord>(stream, PrefixStyle.Base128, 1);
							if (rec.WriteGeneration > startGeneration && rec.Source != m_slotIndex)
								unseenCommits.Add(rec);
							if (rec.WriteGeneration <= minGeneration)
								metadata.Offset = viewOffset + (int) stream.Position;
						}
					}
				}
				curLength += viewLength;
				metadata.Length -= metadata.Offset - viewOffset;
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
