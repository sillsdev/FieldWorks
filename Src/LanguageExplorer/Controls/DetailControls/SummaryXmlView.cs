// Copyright (c) 2005-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Drawing;
using System.Windows.Forms;
using LanguageExplorer.Controls.XMLViews;

namespace LanguageExplorer.Controls.DetailControls
{
	internal class SummaryXmlView : XmlView
	{
		SummarySlice m_slice;

		public SummaryXmlView(int hvo, string label, SummarySlice slice) : base( hvo, label, false)
		{
			m_slice = slice;
		}

		#region IDisposable override

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
		protected override void Dispose(bool disposing)
		{
			// Must not be run more than once.
			if (IsDisposed)
			{
				return;
			}

			base.Dispose(disposing);

			if (disposing)
			{
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_slice = null;
		}

		#endregion IDisposable override

		/// <summary>
		/// Suppress left clicks, except for selecting the slice, and process right clicks by
		/// invoking the menu.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnMouseDown(MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right)
			{
				m_slice.HandleMouseDown(new Point(e.X,e.Y));
			}
			else
			{
				m_slice.ContainingDataTree.CurrentSlice = m_slice;
			}
		}
		public override void MakeRoot()
		{
			base.MakeRoot();
			// pathologically (mainly during Refresh, it seems) the slice width may get set before
			// the root box is created, and no further size adjustment may take place, in which case,
			// when we have made the root, we need to adjust the width it occupies in the parent slice.
			m_slice.AdjustMainViewWidth();
		}

		/// <summary>
		/// Summary slices don't need cursors. The blue context menu icon is sufficient.
		/// </summary>
		protected override void EnsureDefaultSelection()
		{
			// don't set an IP.
		}
	}
}