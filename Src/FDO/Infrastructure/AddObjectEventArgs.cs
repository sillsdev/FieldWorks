using System;

namespace SIL.FieldWorks.FDO.Infrastructure
{
	/// <summary>
	/// This class exists to be the argument to AddObjectSideEffects, and possibly eventually to events which deal
	/// with adding objects to sequences and collections. It provides the information we have so far found to be useful,
	/// but can readily be enhanced with more.
	///
	/// We are considering the possibility of merging this class with one of the FdoStateChange classes so that they can share
	/// all the information required to Undo the change, and thus everything required to inform anyone of what changed.
	/// </summary>
	class AddObjectEventArgs
	{
		private readonly int m_index;

		/// <summary>
		///  Construct one. For the index, pass -1 for a collection and -2 for atomic.
		/// </summary>
		internal AddObjectEventArgs(ICmObject newby, int flid, int index, ICmObject previousOwner) : this(newby, flid, index)
		{
			PreviousOwner = previousOwner;
		}

		/// <summary>
		///  Construct one. For the index, pass -1 for a collection and -2 for atomic.
		/// </summary>
		public AddObjectEventArgs(ICmObject newby, int flid, int index)
		{
			if (newby == null) throw new ArgumentNullException("newby");
			ObjectAdded = newby;
			Flid = flid;
			m_index = index;
		}

		/// <summary>
		/// The object inserted.
		/// </summary>
		public ICmObject ObjectAdded { get; private set; }

		/// <summary>
		/// The field into which the object was inserted.
		/// </summary>
		public int Flid { get; private set; }

		/// <summary>
		/// The place where it was inserted; its current position. Only for sequences.
		/// </summary>
		public int Index
		{
			get
			{
				if (m_index < 0)
					throw new InvalidOperationException("can't get the insert index for a collection");
				return m_index;
			}
		}

		/// <summary>
		/// True if the property is a sequence one; it is then valid to call Index.
		/// </summary>
		public bool IsSequenceField
		{
			get { return m_index >= 0; }
		}

		/// <summary>
		/// The previous owner of the object being added. Or null if irrelevant.
		/// Do not trust that null means there was no previous owner as this is not always set.
		/// </summary>
		public ICmObject PreviousOwner { get; internal set; }
	}
}
