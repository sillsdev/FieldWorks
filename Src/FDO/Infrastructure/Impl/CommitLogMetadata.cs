// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using ProtoBuf;

namespace SIL.FieldWorks.FDO.Infrastructure.Impl
{
	/// <summary>
	/// Contains all of the commit log metadata for the shared XML backend. This
	/// class is serialized to a memory mapped file using protobuf-net.
	/// </summary>
	[ProtoContract]
	internal class CommitLogMetadata
	{
		public CommitLogMetadata()
		{
			Peers = new Dictionary<Guid, CommitLogPeer>();
		}

		/// <summary>
		/// The current commit generation of the commit log.
		/// </summary>
		[ProtoMember(1)]
		public int CurrentGeneration;

		/// <summary>
		/// The current commit generation of the XML file.
		/// </summary>
		[ProtoMember(2)]
		public int FileGeneration;

		/// <summary>
		/// The offset into the memory mapped file where the commit log starts.
		/// </summary>
		[ProtoMember(3)]
		public int LogOffset;

		/// <summary>
		/// The length of the commit log. The length includes padding.
		/// </summary>
		[ProtoMember(4)]
		public int LogLength;

		/// <summary>
		/// The amount of padding at the end of the memory mapped file if the commit log wraps
		/// around the file.
		/// </summary>
		[ProtoMember(5)]
		public int Padding;

		/// <summary>
		/// All of the peers that are currently accessing the XML file.
		/// </summary>
		[ProtoMember(6)]
		public Dictionary<Guid, CommitLogPeer> Peers;

		/// <summary>
		/// The GUID of the master peer that is reponsible for reading and writing to the XML file.
		/// </summary>
		[ProtoMember(7)]
		public Guid Master;
	}

	/// <summary>
	/// A shared XML backend peer.
	/// </summary>
	[ProtoContract]
	internal class CommitLogPeer
	{
		/// <summary>
		/// The commit generation that the peer has seen.
		/// </summary>
		[ProtoMember(1)]
		public int Generation;

		/// <summary>
		/// The peer's process ID.
		/// </summary>
		[ProtoMember(2)]
		public int ProcessID;
	}
}
