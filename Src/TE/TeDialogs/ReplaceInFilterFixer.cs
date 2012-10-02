// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ReplaceInFilterFixer.cs
// Responsibility: lothers
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.Utils;

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

		/// <summary>
		/// Do the actual swap. Also issues a ksyncReloadScriptureControl, which seems to be
		/// needed any time we change the list of active books.
		/// </summary>
		/// <param name="bookOld"></param>
		/// <param name="bookNew"></param>
		private void Swap(IScrBook bookOld, IScrBook bookNew)
		{
			if (m_app != null) // app may be null in tests
			{
				foreach (FwMainWnd wnd in m_app.MainWindows)
					Swap(bookOld, bookNew, wnd);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Swaps the specified bookNew into the book filter and the specified bookOld out of the
		/// filter.
		/// </summary>
		/// <param name="bookOld">The old book to be swapped out of the filter.</param>
		/// <param name="bookNew">The new book to be swapped into the filter.</param>
		/// <param name="mainWnd">The window which contains the filter.</param>
		/// ------------------------------------------------------------------------------------
		private void Swap(IScrBook bookOld, IScrBook bookNew, FwMainWnd mainWnd)
		{
			FilteredScrBooks filter = (FilteredScrBooks)ReflectionHelper.GetProperty(mainWnd, "BookFilter");

			if (filter != null)
				filter.SwapBooks(bookOld, bookNew);
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
			Swap(m_bookOld, m_bookNew);
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
			Swap(m_bookNew, m_bookOld);
			return true;
		}

		#endregion
	}
}
