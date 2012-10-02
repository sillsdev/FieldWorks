// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: BookRevList.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Collections;
using System.Windows.Forms;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Scripture;
//using SIL.FieldWorks.FDO.Cellar;
//using SIL.FieldWorks.Common.ScriptureUtils;
//using SIL.FieldWorks.Common.Controls;
//using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Utils;
//using SIL.FieldWorks.Common.COMInterfaces;
//using System.Diagnostics;
//using SIL.FieldWorks.Resources;
//using SIL.FieldWorks.Common.Framework;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Stores a list of book revisions guaranteed to have unique canonical numbers.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class BookRevList : IFWDisposable, IEnumerable
	{
		#region member variables
		// the list of IScrBook revisions
		private List<IScrBook> m_RevisionList = new List<IScrBook>();
		#endregion

		#region IDisposable & Co. implementation
		// Region last reviewed: never

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// True, if the object has been disposed.
		/// </summary>
		private bool m_isDisposed = false;

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~BookRevList()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <summary>
		///
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Executes in two distinct scenarios.
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing">if set to <c>true</c> [disposing].</param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		protected virtual void Dispose(bool disposing)
		{
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_RevisionList != null)
					m_RevisionList.Clear();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.

			m_isDisposed = true;
		}
		#endregion IDisposable & Co. implementation

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the number of revisions in the list
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Count
		{
			get	{ CheckDisposed(); return m_RevisionList.Count; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets all books in the revision list
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IScrBook[] BookRevs
		{
			get
			{
				CheckDisposed();

				return (m_RevisionList.Count > 0) ? m_RevisionList.ToArray() : null;
			}
		}
		#endregion

		#region Add/Remove Versions
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add a revision to the list of revisions to merge (replaces any existing book having
		/// the same canonical number)
		/// </summary>
		/// <param name="rev">A ScrBook</param>
		/// ------------------------------------------------------------------------------------
		public void AddVersion(IScrBook rev)
		{
			CheckDisposed();

			// If a revision of the same book is already in the list then remove it.
			RemoveVersion(rev.CanonicalNum);
			m_RevisionList.Add(rev);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Remove a revision from the list of revisions to merge.
		/// </summary>
		/// <param name="nBookNumber">The 1-based canonical index of the book to remove</param>
		/// ------------------------------------------------------------------------------------
		public void RemoveVersion(int nBookNumber)
		{
			CheckDisposed();

			// TODO: check to make sure that we don't delete a revision that is
			// currently being merged.
			foreach (IScrBook book in m_RevisionList)
			{
				if (book.CanonicalNum == nBookNumber)
				{
					m_RevisionList.Remove(book);
					break;
				}
			}
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the enumerator.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IEnumerator GetEnumerator()
		{
			return m_RevisionList.GetEnumerator();
		}
	}
}
