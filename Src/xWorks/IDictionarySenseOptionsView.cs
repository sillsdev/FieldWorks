// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using SIL.FieldWorks.FwCoreDlgControls;

namespace SIL.FieldWorks.XWorks
{
	public interface IDictionarySenseOptionsView
	{
		bool NumberMetaConfigEnabled { set; }

		bool SenseInPara { get; set; }

		string NumberingStyle { get; set; }

		string BeforeText { get; set; }

		string AfterText { get; set; }

		string NumberStyle { get; }

		bool NumberSingleSense { get; set; }

		bool ShowGrammarFirst { get; set; }

		/// <summary>Populate the Sense Number Style dropdown</summary>
		void SetStyles(List<StyleComboItem> styles, string selectedStyle);

		event EventHandler BeforeTextChanged;

		event EventHandler NumberingStyleChanged;

		event EventHandler AfterTextChanged;

		event EventHandler NumberSingleSenseChanged;

		event EventHandler NumberStyleChanged;

		/// <summary>Fired when the Styles... button is clicked. Object sender is the Style ComboBox so it can be updated</summary>
		event EventHandler StyleButtonClick;

		event EventHandler ShowGrammarFirstChanged;

		event EventHandler SenseInParaChanged;


	}
}