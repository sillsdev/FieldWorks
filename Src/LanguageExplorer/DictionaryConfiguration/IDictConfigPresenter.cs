// Copyright (c) 2011-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.DictionaryConfiguration
{
	/// <summary>
	/// Interface which the DictionaryConfigManager exposes to either the DictionaryConfigMgrDlg
	/// or the DictionaryConfigViewerStub (the latter for testing).
	/// </summary>
	internal interface IDictConfigPresenter
	{
		IDictConfigViewer Viewer { get; }

		/// <summary>
		/// Get the DictConfigItem associated with this code and mark it for deletion.
		/// </summary>
		/// <returns>true if deletion is successful, false if unsuccessful.</returns>
		bool TryMarkForDeletion(string code);

		/// <summary>
		/// Get the DictConfigItem associated with this code and make a copy of it.
		/// The new item's display name will be "Copy of X", where X is the source item's name.
		/// </summary>
		void CopyConfigItem(string sourceCode);

		/// <summary>
		/// Get the DictConfigItem associated with this code and rename its display name
		/// to the value in newName.
		/// </summary>
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
		bool IsConfigProtected(string code);

		/// <summary>
		/// Tells Viewer whether the configuration it is asking about is a new copy or not.
		/// </summary>
		bool IsConfigNew(string code);

		/// <summary>
		/// The currently selected view (which shoud become the view of the main dialog if OK is clicked)
		/// </summary>
		string FinalConfigurationView { set; }
	}
}
