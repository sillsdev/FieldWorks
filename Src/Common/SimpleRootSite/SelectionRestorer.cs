// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: SelectionRestorer.cs
// Responsibility: FW Team

using System;
using System.Runtime.InteropServices;
using SIL.FieldWorks.Common.COMInterfaces;
using System.Drawing;

namespace SIL.FieldWorks.Common.RootSites
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Helps saving and restoring a selecton around code that could possibly destroy the
	/// selection (or even the location where the selection was located). This is mostly
	/// used around a RefreshDisplay which will reconstruct the whole view from scratch and
	/// will destroy the selection and could, possibly, destroy the text where the selection
	/// was located.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class SelectionRestorer : IDisposable
	{
		/// <summary>The selection that will be restored</summary>
		protected readonly SelectionHelper m_savedSelection;
		/// <summary>The selection that was at the top of the visible area and that will be
		/// scrolled to be the top of the new visible area</summary>
		protected readonly SelectionHelper m_topOfViewSelection;
		/// <summary>The rootsite that will get the selection when restored</summary>
		protected readonly SimpleRootSite m_rootSite;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="SelectionRestorer"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public SelectionRestorer(SimpleRootSite rootSite)
		{
			// we can't use EditingHelper.CurrentSelection here because the scroll position
			// of the selection may have changed.
			m_savedSelection = SelectionHelper.Create(rootSite);
			m_rootSite = rootSite;

			Rectangle rcSrc, rcDst;
			rootSite.GetCoordRects(out rcSrc, out rcDst);
			try
			{
				IVwSelection sel = rootSite.RootBox.MakeSelAt(5, 5, rcSrc, rcDst, false);
				m_topOfViewSelection = SelectionHelper.Create(sel, rootSite);
			}
			catch (COMException)
			{
				// Just ignore any errors
			}
		}

		#region Disposable stuff
		#if DEBUG
		/// <summary/>
		~SelectionRestorer()
		{
			Dispose(false);
		}
		#endif

		/// <summary/>
		public bool IsDisposed { get; private set; }

		/// <summary/>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting
		/// unmanaged resources. In this case, attempt to restore the selection we originally
		/// saved.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void Dispose(bool fDisposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (!fDisposing || IsDisposed || m_savedSelection == null || m_rootSite.RootBox.Height <= 0)
				return;

			if (m_rootSite.ReadOnlyView)
			{
				// if we are a read-only view, then we can't make a writable selection
				RestoreSelectionWhenReadOnly();
				return;
			}

			IVwSelection newSel = RestoreSelection();
			if (newSel == null)
			{
				try
				{
					// Any selection is betther than no selection...
					m_rootSite.RootBox.MakeSimpleSel(true, true, false, true);
				}
				catch (COMException)
				{
					// Just ignore any errors - don't get an selection but who cares.
				}
			}


			IsDisposed = true;
		}

		/// <summary>
		/// This is the normal RestoreSelection. For some reason by default it is not used when read-only.
		/// Returns the selection it successfully restored, or null if it could not restore one.
		/// </summary>
		protected virtual IVwSelection RestoreSelection()
		{
			bool makeVisible = false;
			if (m_topOfViewSelection != null)
			{
				IVwSelection selTop = m_topOfViewSelection.SetSelection(m_rootSite, false, false);
				if (selTop != null && selTop.IsValid)
					m_topOfViewSelection.RestoreScrollPos();
				else
					makeVisible = true;
			}

			return m_savedSelection.MakeBest(makeVisible);
		}

		private void RestoreSelectionWhenReadOnly()
		{
			try
			{
				m_rootSite.RootBox.MakeSimpleSel(true, false, false, true);
			}
			catch (COMException)
			{
				// Just ignore any errors - don't get an selection but who cares.
			}
		}

		#endregion
	}
}
