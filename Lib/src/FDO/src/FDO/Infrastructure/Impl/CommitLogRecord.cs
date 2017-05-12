// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using ProtoBuf;

namespace SIL.FieldWorks.FDO.Infrastructure.Impl
{
	/// <summary>
	/// A record that is used by the shared XML backend to record all information for a commit so that peers
	/// can update their FDO instance appropriately. These records are serialized to the commit log memory
	/// mapped file using protobuf-net.
	/// </summary>
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
		/// A guid unique to one particular peer; used by each peer to eliminate its own commits from queries.
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
