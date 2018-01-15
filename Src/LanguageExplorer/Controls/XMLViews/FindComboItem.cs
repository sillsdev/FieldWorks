// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.Filters;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary />
	internal class FindComboItem : FilterComboItem
	{
		int m_ws;
		FwComboBox m_combo;
		BrowseViewer m_bv;

		/// <summary>
		/// Initializes a new instance of the <see cref="FindComboItem"/> class.
		/// </summary>
		public FindComboItem(ITsString tssName, FilterSortItem fsi, int ws, FwComboBox combo, BrowseViewer bv)
			: base(tssName, null, fsi)
		{
			m_ws = ws;
			m_combo = combo;
			m_bv = bv;
		}

		/// <summary>
		/// Invokes this instance.
		/// </summary>
		public override bool Invoke()
		{
			CheckDisposed();

			IVwStylesheet stylesheet = FontHeightAdjuster.StyleSheetFromPropertyTable(m_bv.PropertyTable);
			using (var dlg = new SimpleMatchDlg(m_combo.WritingSystemFactory, m_bv.PropertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider"), m_ws, stylesheet, m_bv.Cache))
			{
				dlg.SetDlgValues(m_matcher, stylesheet);
				if (dlg.ShowDialog() != DialogResult.OK || dlg.Pattern.Length == 0)
				{
					return false;
				}

				m_matcher = dlg.ResultingMatcher;
				InvokeWithInstalledMatcher();
			}
			return true;
		}

		/// <summary />
		protected internal override void InvokeWithInstalledMatcher()
		{
			// This is a kludge to get around a dubious behavior of combo box: if we set the
			// Tss to something not in the list, and something in the list was previously
			// selected, it fails, making the string empty. If there was already nothing
			// selected, setting the text goes ahead.
			m_combo.SelectedIndex = -1;
			m_combo.Tss = (m_matcher as SimpleStringMatcher).Pattern.Pattern;
			base.InvokeWithInstalledMatcher();
		}

		/// <summary />
		protected override ITsString GetLabelForMatcher()
		{
			return m_combo.Tss;
		}

		internal int Ws => m_ws;

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
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			// Must not be run more than once.
			if (IsDisposed)
			{
				return;
			}

			if (disposing)
			{
			}

			m_combo = null; // Disposed elsewhere.

			base.Dispose (disposing);
		}

		/// <summary>
		/// Determine whether this combo item could have produced the specified matcher.
		/// If so, return the string that should be displayed as the value of the combo box
		/// when this matcher is active. Otherwise return null.
		/// </summary>
		/// <param name="matcher"></param>
		/// <returns></returns>
		internal override ITsString SetFromMatcher(IMatcher matcher)
		{
			CheckDisposed();

			return matcher is SimpleStringMatcher ? matcher.Label : null;
		}

		/// <summary>
		/// Gets or sets the matcher.
		/// </summary>
		/// <value>The matcher.</value>
		public IMatcher Matcher
		{
			get
			{
				CheckDisposed();
				return m_matcher;
			}
			set
			{
				CheckDisposed();
				m_matcher = value;
			}
		}
	}
}