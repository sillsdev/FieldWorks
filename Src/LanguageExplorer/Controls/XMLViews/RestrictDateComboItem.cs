// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.Filters;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary />
	public class RestrictDateComboItem : FilterComboItem
	{
		FwComboBox m_combo;
		int m_ws;
		private IHelpTopicProvider m_helpTopicProvider;
		bool m_fGenDate = false;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:RestrictDateComboItem"/> class.
		/// </summary>
		public RestrictDateComboItem(ITsString tssName, IHelpTopicProvider helpTopicProvider, FilterSortItem fsi, int ws, bool fGenDate, FwComboBox combo) : base(tssName, null, fsi)
		{
			m_helpTopicProvider = helpTopicProvider;
			m_combo = combo;
			m_ws = ws;
			m_fGenDate = fGenDate;
		}

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

			m_combo = null;

			base.Dispose (disposing);
		}

		/// <summary>
		/// Invokes this instance.
		/// </summary>
		public override bool Invoke()
		{
			CheckDisposed();

			using (var dlg = new SimpleDateMatchDlg(m_helpTopicProvider))
			{
				dlg.SetDlgValues(m_matcher);
				dlg.HandleGenDate = m_fGenDate;
				if (dlg.ShowDialog(m_combo) != DialogResult.OK)
				{
					return false;
				}

				m_matcher = dlg.ResultingMatcher;
				m_matcher.WritingSystemFactory = m_combo.WritingSystemFactory;
				m_combo.SelectedIndex = -1; // allows setting text to item not in list, see comment in FindComboItem.Invoke().
				m_combo.Tss = TsStringUtils.MakeString(dlg.Pattern, m_ws);
				var label = m_combo.Tss;
				m_matcher.Label = label;
				// We can't call base.Invoke BEFORE we set the label, because it will persist
				// the wrong label. And we can't call it AFTER we set the label, becaseu it
				// will override our label. So we just copy here a simplified version of the
				// base method. If it gets much more complicated, factor out the common parts
				// into new methods.
				//base.Invoke ();
				m_fsi.Matcher = m_matcher;
				m_fsi.Filter = new FilterBarCellFilter(m_fsi.Finder, m_matcher);
			}

			return true;
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

			return matcher is DateTimeMatcher ? matcher.Label : null;
		}
	}
}