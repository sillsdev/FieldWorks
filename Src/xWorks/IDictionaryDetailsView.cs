// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using SIL.FieldWorks.FwCoreDlgControls;

namespace SIL.FieldWorks.XWorks
{
	public interface IDictionaryDetailsView : IDisposable
	{
		/// <summary>
		/// Tell the controller that the Style selection was changed.
		/// </summary>
		event EventHandler StyleSelectionChanged;

		/// <summary>
		/// Tell the controller the Style button was clicked.
		/// </summary>
		event EventHandler StyleButtonClick;

		/// <summary>
		/// Tell the controller that the Before Text was changed.
		/// </summary>
		event EventHandler BeforeTextChanged;

		/// <summary>
		/// Tell the controller that the Between Text was changed.
		/// </summary>
		event EventHandler BetweenTextChanged;

		/// <summary>
		/// Tell the controller that the After Text was changed.
		/// </summary>
		event EventHandler AfterTextChanged;

		string BeforeText { get; set; }

		string BetweenText { get; set; }

		string AfterText { get; set; }

		string Style { get; }

		bool StylesVisible { set; }

		bool SurroundingCharsVisible { set; get; }

		UserControl OptionsView { set; }

		bool Visible { get; set; }

		Control TopLevelControl { get; }

		bool IsDisposed { get; }

		bool Enabled { get; set; }

		void SetStyles(List<StyleComboItem> styles, string selectedStyle, bool usingParaStyles);

		void SuspendLayout();

		void ResumeLayout();
	}
}
