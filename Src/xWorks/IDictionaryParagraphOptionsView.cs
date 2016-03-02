// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using SIL.FieldWorks.FwCoreDlgControls;

namespace SIL.FieldWorks.XWorks
{
	public interface IDictionaryParagraphOptionsView
	{
		bool StyleCombosEnabled { set; }

		string ParaStyle { get; }

		string ContParaStyle { get; }

		/// <summary>Populate the Paragraph Style dropdown</summary>
		void SetParaStyles(List<StyleComboItem> styles, string selectedStyle);

		/// <summary>Populate the Continuation Paragraph Style dropdown</summary>
		void SetContParaStyles(List<StyleComboItem> styles, string selectedStyle);

		/// <summary>Fired when the Styles... button is clicked. Object sender is the Style ComboBox so it can be updated</summary>
		event EventHandler StyleParaButtonClick;

		event EventHandler StyleContParaButtonClick;

		event EventHandler ParaStyleChanged;

		event EventHandler ContParaStyleChanged;
	}
}
