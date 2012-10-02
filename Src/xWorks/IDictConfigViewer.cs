// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2011' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: IDictConfigViewer.cs
// Responsibility: GordonM
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;

namespace SIL.FieldWorks.XWorks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Interface which the DictionaryConfigMgrDlg or (for test) the DictionaryConfigViewerStub
	/// exposes to the DictionaryConfigManager.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IDictConfigViewer
	{
		IDictConfigPresenter Presenter { get; }

		/// <summary>
		/// Tuples of strings are (uniqueCode, dispName) pairs to be displayed.
		/// </summary>
		/// <param name="listItems"></param>
		/// <param name="selectedItem">The code for the item that should be selected
		/// in the dialog ListView.</param>
		void SetListViewItems(IEnumerable<Tuple<string, string>> listItems, string selectedItem);

		/// <summary>
		/// The unique code for the item currently selected in the dialog listView.
		/// </summary>
		string CurrentSelectedCode { get; }
	}
}
