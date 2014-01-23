// Copyright (c) 2011-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: IDictConfigPresenter.cs
// Responsibility: GordonM
//
// <remarks>
// </remarks>

using System;
using System.Collections.Generic;

namespace SIL.FieldWorks.XWorks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Interface which the DictionaryConfigManager exposes to either the DictionaryConfigMgrDlg
	/// or the DictionaryConfigViewerStub (the latter for testing).
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IDictConfigPresenter
	{
		IDictConfigViewer Viewer { get; }

		/// <summary>
		/// Get the DictConfigItem associated with this code and mark it for deletion.
		/// </summary>
		/// <param name="code"></param>
		/// <returns>true if deletion is successful, false if unsuccessful.</returns>
		bool TryMarkForDeletion(string code);

		/// <summary>
		/// Get the DictConfigItem associated with this code and make a copy of it.
		/// The new item's display name will be "Copy of X", where X is the source item's name.
		/// </summary>
		/// <param name="sourceCode"></param>
		void CopyConfigItem(string sourceCode);

		/// <summary>
		/// Get the DictConfigItem associated with this code and rename its display name
		/// to the value in newName.
		/// </summary>
		/// <param name="code"></param>
		/// <param name="newName"></param>
		void RenameConfigItem(string code, string newName);

		/// <summary>
		/// Sets up stored dictionary configurations as per final values from Viewer.
		/// And change the current selected dictionary view. Prepares Presenter to
		/// communicate to calling dialog (XMLDocConfigureDlg) what needs to be done.
		/// </summary>
		void PersistState();

		/// <summary>
		/// Tells Viewer whether the configuration it is asking about is protected or not.
		/// </summary>
		/// <param name="code"></param>
		/// <returns></returns>
		bool IsConfigProtected(string code);

		/// <summary>
		/// Tells Viewer whether the configuration it is asking about is a new copy or not.
		/// </summary>
		/// <param name="code"></param>
		/// <returns></returns>
		bool IsConfigNew(string code);

		/// <summary>
		/// The currently selected view (which shoud become the view of the main dialog if OK is clicked)
		/// </summary>
		string FinalConfigurationView { set; }
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Interface which the DictionaryConfigManager exposes to its Caller (XmlDocConfigureDlg).
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IDictConfigManager
	{
		/// <summary>
		/// If there has been no change in change in current view, this will be null.
		/// If a new configuration should now be the current one (a copy was made or
		/// the current one was deleted), then this will return the unique code of the
		/// new current view.
		/// </summary>
		string FinalConfigurationView { get; }

		/// <summary>
		/// If copies of older configuration views have been made, this property will
		/// provide a list of the new views to create.
		/// Items(Tuples) in the list are of the format:
		///		(newUniqueCode, codeOfViewCopiedFrom, newDisplayName)
		/// </summary>
		IEnumerable<Tuple<string, string, string>> NewConfigurationViews { get; }

		/// <summary>
		/// If existing configuration views have been deleted, this property will
		/// provide a list of the unique codes to delete.
		/// N.B.: Make sure Caller processes copying views first, in case some of
		/// the copies are based on views that are to be deleted!
		/// </summary>
		IEnumerable<string> ConfigurationViewsToDelete { get; }

		/// <summary>
		/// If older configuration views have been renamed, this property will
		/// provide a list of the codes with their new display names.
		/// Items(Tuples) in the list are of the format:
		///		(uniqueCode, newDisplayName)
		/// </summary>
		IEnumerable<Tuple<string, string>> RenamedExistingViews { get; }
	}
}
