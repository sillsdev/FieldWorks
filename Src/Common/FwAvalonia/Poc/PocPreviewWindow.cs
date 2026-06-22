// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using Avalonia.Automation;
using Avalonia.Controls;

namespace SIL.FieldWorks.Common.FwAvalonia.Poc
{
	/// <summary>
	/// Net48 preview-host window for the lexical-edit POC slice. The host sets the DataContext from
	/// <see cref="PocPreviewDataProvider"/>; this window responds by creating a fresh slice for that
	/// data and exposing stable automation identifiers for UIA tests.
	/// </summary>
	public sealed class PocPreviewWindow : Window
	{
		public PocPreviewWindow()
		{
			Width = 900;
			Height = 520;
			AutomationProperties.SetAutomationId(this, "LexicalEditPocWindow");
			AutomationProperties.SetName(this, "Lexical Edit POC Preview");

			var empty = new PocLexEntrySlice(PocEntryDto.CreateSample());
			Content = empty;
		}

		protected override void OnDataContextChanged(System.EventArgs e)
		{
			base.OnDataContextChanged(e);
			if (DataContext is PocEntryDto entry)
			{
				Content = new PocLexEntrySlice(entry);
			}
		}
	}
}
