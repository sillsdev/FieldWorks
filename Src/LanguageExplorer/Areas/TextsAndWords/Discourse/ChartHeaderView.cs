// Copyright (c) 2008-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorer.Areas.TextsAndWords.Discourse
{
	/// <summary>
	/// This subclass of ListView is used to make the column headers for a Constituent Chart.
	/// It's main function is to handle double-clicks on column boundaries so the chart (which is neither
	/// a ListView nor a BrowseViewer) can resize its columns.
	/// </summary>
	internal class ChartHeaderView : ListView
	{
		private ConstituentChart m_chart;

		/// <summary>
		/// Create one and set the chart it belongs to.
		/// </summary>
		/// <param name="chart"></param>
		public ChartHeaderView(ConstituentChart chart)
		{
			m_chart = chart;
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

		/// <summary>
		/// Releases the unmanaged resources used by the <see cref="T:System.Windows.Forms.ListView"/> and optionally releases the managed resources.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
			{
				return;
			}

			if (disposing)
			{
			}
			m_chart = null;

			base.Dispose(disposing);
		}

		const int WM_NOTIFY = 0x004E;
		const int HDN_FIRST = -300;
		const int HDN_DIVIDERDBLCLICKW = (HDN_FIRST - 25);

		/// <summary>
		/// Overrides <see cref="M:System.Windows.Forms.Control.WndProc(System.Windows.Forms.Message@)"/>.
		/// </summary>
		protected override void WndProc(ref Message m)
		{
			switch (m.Msg)
			{
				case WM_NOTIFY:
					var nmhdr = (Win32.NMHEADER)m.GetLParam(typeof(Win32.NMHEADER));
					switch (nmhdr.hdr.code)
					{
						case HDN_DIVIDERDBLCLICKW:
							// double-click on line between column headers.
							// adjust width of column to match item of greatest length.
							m_chart.m_headerMainCols_ColumnAutoResize(nmhdr.iItem);
							break;
						default:
							base.WndProc(ref m);
							break;
					}

					break;
				default:
					base.WndProc(ref m);
					break;
			}
		}
	}
}