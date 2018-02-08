// Copyright (c) 2008-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary />
	public class TargetColumnChangedEventArgs : EventArgs
	{
		private readonly TargetFieldItem m_tfi;

		internal TargetColumnChangedEventArgs(TargetFieldItem selectedTargetFieldItem)
		{
			m_tfi = selectedTargetFieldItem;
		}

		/// <summary>
		/// Target column expects its base list to be this list items class
		/// </summary>
		public int ExpectedListItemsClass => m_tfi.ExpectedListItemsClass;

		/// <summary>
		/// The field we want to bulk edit (or 0, if it doesn't matter).
		/// </summary>
		public int TargetFlid => m_tfi.TargetFlid;

		internal int ColumnIndex => m_tfi.ColumnIndex;

		/// <summary>
		/// True to force reload of list even if expected item class has not changed.
		/// </summary>
		public bool ForceReload { get; internal set; }
	}

	/// <summary>
	/// Notify clients that a bulk edit target column has changed and readjust to
	/// new list items class if necessary.
	/// </summary>
	public delegate void TargetColumnChangedHandler(object sender, TargetColumnChangedEventArgs e);
}