// Copyright (c) 2016-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;

namespace LanguageExplorer
{
	/// <summary>
	/// Interface for a record bar.
	/// </summary>
	public interface IRecordBar : IDisposable
	{
		/// <summary>
		/// Get the TreeView control, or null, if using a ListView.
		/// </summary>
		TreeView TreeView { get; }

		/// <summary>
		/// Get the ListView control, or null, if using a TreeView.
		/// </summary>
		ListView ListView { get; }

		/// <summary>
		/// Use 'true' to show as the ListView, otherwise 'false' to show the TreeView.
		/// </summary>
		bool IsFlatList { set; }

		/// <summary>
		/// 'true' if the control has the optional header control, otherwise 'false'.
		/// </summary>
		bool HasHeaderControl { get; }

		/// <summary>
		/// 'true' to show the optional header control, otherwsie 'false' to hide it.
		/// </summary>
		/// <remarks>Has no affect, if there is no header control.</remarks>
		bool ShowHeaderControl { set; }

		/// <summary>
		/// Add an optional header control
		/// </summary>
		/// <param name="c">An optional header control.</param>
		void AddHeaderControl(Control c);

		/// <summary>
		/// Select the given TreeNode (when showing the TreeView).
		/// </summary>
		TreeNode SelectedNode { set; }

		/// <summary>
		/// Clear both views.
		/// </summary>
		void Clear();
	}
}
