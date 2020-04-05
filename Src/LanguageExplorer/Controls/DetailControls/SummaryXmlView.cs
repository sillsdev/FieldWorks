// Copyright (c) 2005-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using LanguageExplorer.Controls.XMLViews;

namespace LanguageExplorer.Controls.DetailControls
{
	internal sealed class SummaryXmlView : XmlView
	{
		private ISummarySlice _summarySlice;

		internal SummaryXmlView(int hvo, string label, ISummarySlice summarySlice) : base( hvo, label, false)
		{
			_summarySlice = summarySlice;
		}

		#region IDisposable override

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			base.Dispose(disposing);

			if (disposing)
			{
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			_summarySlice = null;
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
				_summarySlice.HandleMouseDown(new Point(e.X,e.Y));
			}
			else
			{
				_summarySlice.ContainingDataTree.CurrentSlice = _summarySlice;
			}
		}

		public override void MakeRoot()
		{
			base.MakeRoot();
			// pathologically (mainly during Refresh, it seems) the slice width may get set before
			// the root box is created, and no further size adjustment may take place, in which case,
			// when we have made the root, we need to adjust the width it occupies in the parent slice.
			_summarySlice.AdjustMainViewWidth();
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