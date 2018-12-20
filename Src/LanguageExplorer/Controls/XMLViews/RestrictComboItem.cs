// Copyright (c) 2004-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using LanguageExplorer.Filters;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary />
	public class RestrictComboItem : FilterComboItem
	{
		FwComboBox m_combo;
		int m_ws;
		private IHelpTopicProvider m_helpTopicProvider;

		/// <summary />
		public RestrictComboItem(ITsString tssName, IHelpTopicProvider helpTopicProvider, FilterSortItem fsi, int ws, FwComboBox combo) : base(tssName, null, fsi)
		{
			m_helpTopicProvider = helpTopicProvider;
			m_combo = combo;
			m_ws = ws;
		}

		/// <inheritdoc />
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

			base.Dispose(disposing);
		}

		/// <summary>
		/// Invokes this instance.
		/// </summary>
		public override bool Invoke()
		{
			using (var dlg = new SimpleIntegerMatchDlg(m_helpTopicProvider))
			{
				dlg.SetDlgValues(m_matcher);
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
		internal override ITsString SetFromMatcher(IMatcher matcher)
		{
			return matcher is IntMatcher ? matcher.Label : null;
		}
	}
}