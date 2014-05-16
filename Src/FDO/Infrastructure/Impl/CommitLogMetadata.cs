using System;
using System.Collections.Generic;
using ProtoBuf;

namespace SIL.FieldWorks.FDO.Infrastructure.Impl
{
	[ProtoContract]
	internal class CommitLogMetadata
	{
		[ProtoMember(1)]
		public int CurrentGeneration;

		[ProtoMember(2)]
		public int FileGeneration;

		[ProtoMember(3)]
		public int Offset;

		[ProtoMember(4)]
		public int Length;

		[ProtoMember(5)]
		public int Padding;

		[ProtoMember(6)]
		public Dictionary<Guid, CommitLogPeer> Peers;

		[ProtoMember(7)]
		public Guid Master;
	}

	[ProtoContract]
	internal class CommitLogPeer
	{
		[ProtoMember(1)]
		public int Generation;

		[ProtoMember(2)]
		public int ProcessID;
	}
}
