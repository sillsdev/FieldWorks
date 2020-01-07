// Copyright (c) 2006-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using LanguageExplorer.Controls;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	/// <summary>
	/// encapsulates the common behavior of items in an InterlinComboHandler combo list.
	/// </summary>
	internal class InterlinComboHandlerActionComboItem : HvoTssComboItem
	{
		EventHandler OnSelect;

		/// <summary />
		/// <param name="tssDisplay">the tss used to display the text of the combo item.</param>
		/// <param name="select">the event delegate to be executed when this item is selected. By default,
		/// we send "this" InterlinComboHandlerActionComboItem as the event sender.</param>
		internal InterlinComboHandlerActionComboItem(ITsString tssDisplay, EventHandler select)
			: this(tssDisplay, select, 0, 0)
		{
		}

		/// <summary />
		/// <param name="tssDisplay">the tss to display in the combo box.</param>
		/// <param name="select">the event to fire when this is selected</param>
		/// <param name="hvoPrimary">the hvo most closely associated with this item, 0 if none.</param>
		/// <param name="tag">id to resolve any further ambiguity associated with this item's hvo.</param>
		internal InterlinComboHandlerActionComboItem(ITsString tssDisplay, EventHandler select, int hvoPrimary, int tag)
			: base(hvoPrimary, tssDisplay, tag)
		{
			OnSelect = select;
		}

		/// <summary>
		/// If enabled, will do something if clicked.
		/// </summary>
		internal bool IsEnabled => OnSelect != null;

		/// <summary>
		/// Do OnSelect if defined, and this item is enabled.
		/// By default, we send "this" InterlinComboHandlerActionComboItem as the event sender.
		/// </summary>
		protected internal virtual void OnSelectItem()
		{
			if (OnSelect != null && IsEnabled)
			{
				OnSelect(this, EventArgs.Empty);
			}
		}
	}
}