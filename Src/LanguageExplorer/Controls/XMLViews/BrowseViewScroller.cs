// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// This class manages the parts of the BrowseViewer that scroll horizontally in sync.
	/// </summary>
	internal class BrowseViewScroller : UserControl
	{
		BrowseViewer m_bv;

		/// <summary />
		public BrowseViewScroller(BrowseViewer bv)
		{
			m_bv = bv;
		}

		/// <summary />
		protected override void OnLayout(LayoutEventArgs levent)
		{
#if __MonoCS__ // FWNX-425
			m_bv.EnsureScrollContainerIsCorrectWidth();
#endif

			m_bv.LayoutScrollControls();
			// It's important to do this AFTER laying out the embedded controls, because it figures
			// out whether to display the scroll bar based on their sizes and positions.
			base.OnLayout (levent);
		}

#if __MonoCS__ // FWNX-425
#pragma warning disable 1587
		/// <summary> </summary>
#pragma warning restore 1587
		protected override void OnSizeChanged(EventArgs e)
		{
			m_bv.EnsureScrollContainerIsCorrectWidth();

			base.OnSizeChanged(e);
		}
#endif

		/// <summary />
		protected override void OnMouseWheel(MouseEventArgs e)
		{
			// Suppress horizontal scrolling if we are doing vertical.
			if (m_bv?.ScrollBar != null && m_bv.ScrollBar.Maximum >= m_bv.ScrollBar.LargeChange)
			{
				return;
			}
			base.OnMouseWheel(e);
		}

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
			{
				throw new ObjectDisposedException($"'{GetType().Name}' in use after being disposed.");
			}
		}

		/// <summary />
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + ". ******************");
			// Must not be run more than once.
			if (IsDisposed)
			{
				return;
			}

			if (disposing)
			{
			}
			m_bv = null;

			base.Dispose(disposing);
		}
	}
}