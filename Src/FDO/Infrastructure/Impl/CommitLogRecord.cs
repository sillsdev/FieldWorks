using System;
using System.Collections.Generic;
using ProtoBuf;

namespace SIL.FieldWorks.FDO.Infrastructure.Impl
{
	[ProtoContract]
	internal struct CommitLogRecord
	{
		/// <summary>
		/// Corresponds to WriteGeneration in CmObjectSurrogate. Orders all CommitData absolutely, since each
		/// Commit gets a fresh WriteGeneration.
		/// </summary>
		[ProtoMember(1)]
		public int WriteGeneration;
		/// <summary>
		/// A guid unique to one particular writer; used by each client to eliminate its own commits from queries.
		/// </summary>
		[ProtoMember(2)]
		public Guid Source;
		/// <summary>
		/// Our IDs of the objects that were added.
		/// </summary>
		[ProtoMember(3)]
		public List<byte[]> ObjectsAdded;
		/// <summary>
		/// Our IDs of the objects that were modified.
		/// </summary>
		[ProtoMember(4)]
		public List<byte[]> ObjectsUpdated;
		/// <summary>
		/// Our IDs of the objects that were deleted.
		/// </summary>
		[ProtoMember(5)]
		public List<Guid> ObjectsDeleted;
	}
}
