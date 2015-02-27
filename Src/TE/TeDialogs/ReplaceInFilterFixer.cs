// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ReplaceInFilterFixer.cs
// Responsibility: lothers
//
// <remarks>
// </remarks>

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.TE
{
	/// <summary>
	/// Handles the problem of replacing a current book with an alternative. (Either may be null.)
	/// Finds affected filter (currently assumes only the active main window needs fixing), and
	/// updates it appropriately.
	/// Originally written to swap them in a single operation, it turns out that doesn't work, because
	/// when (Re)doing, we want the sequence, remove old from filter, delete, copy, add new to filter; and on
	/// Undo we want remove new from filter, undo copy, undo delete, add old to filter.
	/// Leaving it this way in case it's ever useful, and because it works fine just to make two of them,
	/// each with one of the hvos zero.
	/// </summary>
	public class ReplaceInFilterFixer : IUndoAction
	{
		private readonly IScrBook m_bookOld;
		private readonly IScrBook m_bookNew;
		private readonly FwApp m_app;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make one
		/// </summary>
		/// <param name="bookOld">The book old.</param>
		/// <param name="bookNew">The book new.</param>
		/// <param name="app">The app.</param>
		/// ------------------------------------------------------------------------------------
		public ReplaceInFilterFixer(IScrBook bookOld, IScrBook bookNew, FwApp app)
		{
			m_bookOld = bookOld;
			m_bookNew = bookNew;
			m_app = app;
		}

		#region IUndoAction Members

		/// <summary>
		/// All done.
		/// </summary>
		public void Commit()
		{
		}

		/// <summary>
		/// Nothing changes in DB
		/// </summary>
		/// <returns></returns>
		public bool IsDataChange
		{
			get
			{
				// Although this action isn't technically a data change, the time it needs to
				// be called during the undo/redo process is the same as if it were a data change.
				// So instead of coming up with another property (like a
				// "NeedsToHappenRightBeforePropChanged") we just opted to returning true here.
				// (FWR-1802).
				return true;
			}
		}

		/// <summary>
		/// Yes we can
		/// </summary>
		/// <returns></returns>
		public bool IsRedoable
		{
			get { return true; }
		}

		/// <summary>
		/// Repeat original swap, replacing old with new in filter. Also used for original Do.
		/// </summary>
		/// <returns></returns>
		public bool Redo()
		{
			return true;
		}

		/// <summary>
		/// Ignore
		/// </summary>
		public bool SuppressNotification
		{
			set { }
		}

		/// <summary>
		/// Reverse the change, removing the new Hvo and inserting the old.
		/// </summary>
		/// <returns></returns>
		public bool Undo()
		{
			return true;
		}

		#endregion
	}
}
