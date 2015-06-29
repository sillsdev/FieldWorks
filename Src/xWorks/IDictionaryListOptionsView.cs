using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SIL.FieldWorks.XWorks.DictionaryDetailsView
{
	public interface IDictionaryListOptionsView : IDisposable
	{
		/// <summary>
		/// Returns all items in the list control
		/// </summary>
		List<ListViewItem> AvailableItems { get; set; }

		/// <summary>Label for the "DisplayOption" CheckBox below the list, eg "Disp WS Abbrevs" or "Disp Complex Forms in Paragraphs"</summary>
		string DisplayOptionCheckBoxLabel { set; }

		/// <summary>Label for the list, eg "Writing Systems:" or "Complex Form Types:"</summary>
		string ListViewLabel { set; }

		/// <summary>Whether or not the single "display option" checkbox below the list is checked</summary>
		bool DisplayOptionCheckBoxChecked { get; set; }

		/// <summary>
		/// Enabled option set to MoveUp button
		/// </summary>
		bool MoveUpEnabled { set; }

		/// <summary>
		/// Enabled option set to MoveDown button
		/// </summary>
		bool MoveDownEnabled { set; }

		/// <summary>Whether or not the single "display option" checkbox below the list is visible</summary>
		bool DisplayOptionCheckBoxVisible { set; }

		/// <summary>Whether or not the single "display option" checkbox below the list is enabled</summary>
		bool DisplayOptionCheckBoxEnabled { get; set; }

		/// <note>
		/// Although it seems daft to hide the ListView in a ListOptionsView, Referenced Complex Forms always uses the single checkbox,
		/// but only sometimes uses the list of checkboxes.  So we hide the list when it is not in use.
		/// </note>
		bool ListViewVisible { set; }

		/// <summary>
		/// Event will call when the MoveUp button clicked
		/// </summary>
		event EventHandler UpClicked;

		/// <summary>
		/// Event will call when the MoveDown button clicked
		/// </summary>
		event EventHandler DownClicked;

		/// <summary>
		/// Event will call when selected item index has changed
		/// </summary>
		event ListViewItemSelectionChangedEventHandler ListItemSelectionChanged;

		/// <summary>
		/// Event will call when checkbox item has been checked / unchecked
		/// </summary>
		event ItemCheckedEventHandler ListItemCheckBoxChanged;

		/// <summary>
		/// UserControl's Load event will call
		/// </summary>
		event EventHandler Load;

		/// <summary>EventHandler for the single "display option" checkbox below the list</summary>
		event EventHandler DisplayOptionCheckBoxChanged;



	}
}