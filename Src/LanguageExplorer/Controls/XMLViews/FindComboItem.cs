// Copyright (c) 2004-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using System.Windows.Forms;
using LanguageExplorer.Filters;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary />
	internal class FindComboItem : FilterComboItem
	{
		private FwComboBox m_combo;
		private BrowseViewer m_bv;

		/// <summary />
		public FindComboItem(ITsString tssName, FilterSortItem fsi, int ws, FwComboBox combo, BrowseViewer bv)
			: base(tssName, null, fsi)
		{
			Ws = ws;
			m_combo = combo;
			m_bv = bv;
		}

		/// <summary>
		/// Invokes this instance.
		/// </summary>
		public override bool Invoke()
		{
			IVwStylesheet stylesheet = FwUtils.StyleSheetFromPropertyTable(m_bv.PropertyTable);
			using (var dlg = new SimpleMatchDlg(m_combo.WritingSystemFactory, m_bv.PropertyTable.GetValue<IHelpTopicProvider>(LanguageExplorerConstants.HelpTopicProvider), Ws, stylesheet, m_bv.Cache))
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

		internal int Ws { get; }

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");

			if (disposing)
			{
			}

			m_combo = null; // Disposed elsewhere.

			base.Dispose(disposing);
		}

		/// <summary>
		/// Determine whether this combo item could have produced the specified matcher.
		/// If so, return the string that should be displayed as the value of the combo box
		/// when this matcher is active. Otherwise return null.
		/// </summary>
		internal override ITsString SetFromMatcher(IMatcher matcher)
		{
			return matcher is SimpleStringMatcher ? matcher.Label : null;
		}

		/// <summary>
		/// Gets or sets the matcher.
		/// </summary>
		public IMatcher Matcher
		{
			get
			{
				return m_matcher;
			}
			set
			{
				m_matcher = value;
			}
		}
	}
}