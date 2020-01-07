// Copyright (c) 2005-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace LanguageExplorer.Controls.DetailControls
{
	internal class LiteralLabelView : RootSiteControl
	{
		private string m_text;
		private LiteralLabelVc m_vc;
		private SummarySlice m_slice;

		public LiteralLabelView(string text, SummarySlice slice)
		{
			m_text = text;
			m_slice = slice;
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
			m_vc = null;
			m_text = null;
			m_slice = null;
		}

		#endregion IDisposable override

		public override void MakeRoot()
		{
			if (m_cache == null || DesignMode)
			{
				return;
			}
			base.MakeRoot();
			m_vc = new LiteralLabelVc(m_text, m_cache.WritingSystemFactory.UserWs);
			RootBox.DataAccess = m_cache.DomainDataByFlid;
			// Since the VC just displays a literal, both the root HVO and the root frag are arbitrary.
			RootBox.SetRootObject(1, m_vc, 2, StyleSheet);
			// pathologically (mainly during Refresh, it seems) the slice width may get set before
			// the root box is created, and no further size adjustment may take place, in which case,
			// when we have made the root, we need to adjust the width it occupies in the parent slice.
			m_slice.AdjustMainViewWidth();
		}

		/// <summary>
		/// Suppress left clicks, except for selecting the slice, and process right clicks by
		/// invoking the menu.
		/// </summary>
		protected override void OnMouseDown(MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right)
			{
				m_slice.HandleMouseDown(new Point(e.X, e.Y));
			}
			else
			{
				m_slice.ContainingDataTree.CurrentSlice = m_slice;
			}
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