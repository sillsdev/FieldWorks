// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using SIL.FieldWorks.Common.ViewsInterfaces;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// This class maintains the vertical scroll position in the BrowseViewer when a RootBox.Reconstruct() is necessary.
	/// It is intended to be used in a using() construct, so that its Dispose() forces a RootBox.Reconstruct()
	/// at the end of the using block and then makes sure the scroll position is valid.
	/// </summary>
	internal class ReconstructPreservingBVScrollPosition : IDisposable
	{
		BrowseViewer m_bv;
		int m_irow;
		bool m_fHiliteWasVisible;

		/// <summary>
		/// Ctor saves BrowseViewer Scroll Position. Dispose(true) does RootBox.Reconstruct() and restores scroll position.
		/// </summary>
		public ReconstructPreservingBVScrollPosition(BrowseViewer bv)
		{
			m_bv = bv;
			// Store location for restore after Reconstruct. (LT-8336)
			m_bv.BrowseView.SaveScrollPosition(null);

			// Figure out if highlighted row is visible or not
			m_irow = m_bv.SelectedIndex;
			m_fHiliteWasVisible = false;
			if (m_irow < 0)
			{
				return;
			}
			var sel = MakeTestRowSelection(m_irow);
			if (sel == null)
			{
				return;
			}

			if (m_bv.BrowseView.IsSelectionVisible(sel))
			{
				m_fHiliteWasVisible = true;
			}
		}

		private IVwSelection MakeTestRowSelection(int iselRow)
		{
			var rgvsli = new SelLevInfo[1];
			rgvsli[0].ihvo = iselRow;
			rgvsli[0].tag = m_bv.MainTag;
			return m_bv.BrowseView.RootBox.MakeTextSelInObj(0, 1, rgvsli, 0, null, false, false, false, true, false);
		}

		#region IDisposable & Co. implementation

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed { get; private set; }

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~ReconstructPreservingBVScrollPosition()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <summary />
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

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected virtual void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + ". ******************");
			// Must not be run more than once.
			if (IsDisposed)
			{
				return;
			}

			if (disposing)
			{
				// Dispose managed resources here.
				// Do this in the context of Scenario 2,
				// since in #1 the m_bv may have been finalized already.
				// Restore scroll position here
				m_bv?.BrowseView?.RootBox?.Reconstruct(); // Otherwise every cell redraws individually!

				m_bv.BrowseView.RestoreScrollPosition(null);

				if (m_fHiliteWasVisible && m_irow >= 0 && m_irow < m_bv.AllItems.Count)
				{
					// If there WAS a highlighted row visible and it is no longer visible, scroll to make it so.
					var newSel = MakeTestRowSelection(m_irow);
					if (newSel != null && !m_bv.BrowseView.IsSelectionVisible(newSel)) // Need to scroll newSel into view
					{
						m_bv.BrowseView.RestoreScrollPosition(Math.Max(0, m_irow - 2));
					}
				}

				m_bv = null;
			}

			// Dispose unmanaged resources here, whether disposing is true or false.

			IsDisposed = true;
		}

		/// <summary>
		/// Throw if the IsDisposed property is true
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
			{
				throw new ObjectDisposedException(ToString(), "This object is being used after it has been disposed: this is an Error.");
			}
		}

		#endregion IDisposable & Co. implementation
	}
}