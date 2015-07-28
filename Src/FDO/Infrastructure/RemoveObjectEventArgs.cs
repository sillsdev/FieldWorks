// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace SIL.FieldWorks.FDO.Infrastructure
{
	/// <summary>
	/// This class exists to be the argument to RemoveObjectSideEffects, and possibly eventually to events which deal
	/// with removing objects to sequences and collections. It provides the information we have so far found to be useful,
	/// but can readily be enhanced with more.
	///
	/// We are considering the possibility of merging this class with one of the FdoStateChange classes so that they can share
	/// all the information required to Undo the change, and thus everything required to inform anyone of what changed.
	///
	/// We may also end up with a proper class hierarchy, too; there is a good deal of overlap with AddObjectEventArgs.
	/// </summary>
	class RemoveObjectEventArgs
	{
		private readonly int m_index;

		/// <summary>
		///  Construct one for object deletion. For the index, pass -1 for a collection and -2 for atomic.
		/// </summary>
		public RemoveObjectEventArgs(ICmObject goner, int flid, int index)
			:this(goner, flid, index, true)
		{}

		/// <summary>
		///  Construct one. For the index, pass -1 for a collection and -2 for atomic.
		/// </summary>
		public RemoveObjectEventArgs(ICmObject goner, int flid, int index, bool forDeletion)
			: this(goner, flid, index, forDeletion, false)
		{
		}

		/// <summary>
		///  Construct one. For the index, pass -1 for a collection and -2 for atomic.
		/// </summary>
		public RemoveObjectEventArgs(ICmObject goner, int flid, int index, bool forDeletion, bool delaySideEffects)
		{
			if (goner == null) throw new ArgumentNullException("goner");
			ObjectRemoved = goner;
			Flid = flid;
			m_index = index;
			ForDeletion = forDeletion;
			DelaySideEffects = delaySideEffects;
		}

		/// <summary>
		/// The object removed.
		/// </summary>
		public ICmObject ObjectRemoved { get; private set; }

		/// <summary>
		/// The field from which the object was removed.
		/// </summary>
		public int Flid { get; private set; }

		/// <summary>
		/// The place where it was removed; its previous position. Only for sequences.
		/// </summary>
		public int Index
		{
			get
			{
				if (m_index < 0)
					throw new InvalidOperationException("can't get the remove index for a collection or atomic");
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
		/// True if the object is being deleted; false if it is being moved.
		/// </summary>
		public bool ForDeletion { get; private set; }

		/// <summary>
		/// True if the object's removal will generate side effects which should
		/// be delayed until after the object is inserted into its new owner.
		/// Used for members of owned sequences where removal triggers (for example)
		/// deletion of its owner, as in the case of deleting a cellpart from a chart row.
		/// N.B.: The normal RemoveObjectSideEffectsInternal() call will be made before
		/// the object is actually moved too, but you can test for this condition in order
		/// to get it called after the move too.
		/// </summary>
		public bool DelaySideEffects { get; private set; }
	}
}
