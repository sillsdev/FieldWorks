// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using SIL.LCModel.Core.KernelInterfaces;

namespace SIL.FieldWorks.Common.Widgets
{
	/// <summary>
	/// IComboList describes the shared methods of FwComboBox and ComboListBox, allowing the two classes
	/// to be more easily used as alternates for similar purposes.
	/// </summary>
	public interface IComboList
	{
		/// <summary />
		event EventHandler SelectedIndexChanged;

		/// <summary>
		/// Gets or sets the drop down style.
		/// </summary>
		/// <value>The drop down style.</value>
		ComboBoxStyle DropDownStyle { get; set; }
		/// <summary />
		int SelectedIndex { get; set; }
		/// <summary />
		string Text { get; set; }
		/// <summary />
		ObjectCollection Items { get; }
		/// <summary />
		int FindStringExact(string str);
		/// <summary>
		/// Get or set the writing system factory used to interpret strings. It's important to set this
		/// if using TsStrings for combo items.
		/// </summary>
		ILgWritingSystemFactory WritingSystemFactory { get; set ; }
		/// <summary>
		/// Get/Set the selected item from the list; null if none is selected.
		/// </summary>
		object SelectedItem { get; set; }
		/// <summary>
		/// Gets or sets the style sheet.
		/// </summary>
		IVwStylesheet StyleSheet { get; set; }
	}
}