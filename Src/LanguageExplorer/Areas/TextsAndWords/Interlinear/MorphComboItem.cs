// Copyright (c) 2006-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	internal class MorphComboItem : InterlinComboHandlerActionComboItem
	{
		internal MorphComboItem(MorphItem mi, ITsString tssDisplay, EventHandler handleMorphComboItem, int hvoPrimary)
			: base(tssDisplay, handleMorphComboItem, hvoPrimary, 0)
		{
			MorphItem = mi;
		}

		/// <summary>
		/// A list of MorphItems, which contain both the main-cache hvo of the
		/// MoForm with the right text in the m_wsVern alternative of its MoForm_Form, and
		/// the main-cache hvo of each sense of that MoForm.  A sense hvo of 0 is used to
		/// flag the "Add New Sense" line which ends each MoForm's list of sense.
		/// </summary>
		internal MorphItem MorphItem { get; }
	}
}