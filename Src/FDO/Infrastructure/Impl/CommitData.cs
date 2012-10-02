using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIL.FieldWorks.FDO.Infrastructure.Impl
{
	/// <summary>
	/// This class is used with the db4o backend to record the key information about what changed in a transaction.
	/// </summary>
	class CommitData
	{
		/// <summary>
		/// Corresponds to WriteGeneration in CmObjectSurrogate. Orders all CommitData absolutely, since each
		/// Commit gets a fresh WriteGeneration.
		/// </summary>
		public int WriteGeneration;
		/// <summary>
		/// A guid unique to one particular writer; used by each client to eliminate its own commits from queries.
		/// </summary>
		public Guid Source;
		/// <summary>
		/// Db4o object IDs of all the CmObjects added by the transaction. We use these rather than
		/// GUIDs because we can use this to retrieve the object, and from that get the guid, but
		/// we can't get anywhere starting with the GUID of an object we've never seen.
		/// </summary>
		public long[] ObjectsAdded;
		/// <summary>
		/// Our IDs of the objects that were modified.
		/// </summary>
		public Guid[] ObjectsUpdated;
		/// <summary>
		/// Our IDs of the objects that were deleted.
		/// </summary>
		public Guid[] ObjectsDeleted;
	}
}
