// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using SIL.FieldWorks.Common.DetailRules;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.FieldWorks.LexText.Controls;
using SIL.LCModel;
using SIL.Reporting;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// The host half of Category Edit's item menus: Duplicate and Move on the rows of an affix
	/// slot or an affix template.
	/// </summary>
	public partial class RecordEditView
	{
		/// <summary>The tool whose Insert menu commands this view answers for a
		/// category.</summary>
		internal const string CategoryEditTool = "posEdit";

		/// <summary>The message every Duplicate leaf of the detail menus sends.</summary>
		internal const string DuplicateMessage = "DataTreeCopy";

		/// <summary>The message of Move Affix Slot and Move Affix Template.</summary>
		internal const string MoveToCategoryMessage = "DataTreeMove";

		/// <summary>
		/// Picks the category to move a slot or template to, given the category it is in and
		/// the command's label, or null to cancel: the product asks with
		/// <see cref="LcmCategoryChooserLauncher"/>; a headless test answers for the user.
		/// </summary>
		internal Func<IPartOfSpeech, string, IPartOfSpeech> MoveDestinationChosen { get; set; }

		/// <summary>
		/// The Duplicate item for the row's object: absent on a row with no object, a ghost
		/// row and a row whose object the command does not act on; disabled when
		/// <see cref="DetailObjectDuplication.CanDuplicate"/> refuses the object.
		/// </summary>
		internal DetailMenuItem DuplicateItem(DetailMenuLeaf leaf, DetailField field)
		{
			var target = RowObjectOf(field);
			if (target == null || field.GhostPrompt != null || !CommandActsOn(target, leaf.Command))
				return null;
			var label = XCoreMenuBridge.StripAccelerator(leaf.Label);
			if (!DetailObjectDuplication.CanDuplicate(target))
				return DetailMenuItem.Disabled(label);
			return new DetailMenuItem(label, isEnabled: true, isChecked: false, children: null,
				execute: () => DuplicateDetailObject(field, leaf.Command));
		}

		/// <summary>
		/// Duplicates the row's object beside it: pending edits settle first, the copy is one
		/// undoable step, and the re-show focuses the copy's first row.
		/// </summary>
		/// <returns>The copy, or null when nothing was duplicated.</returns>
		internal ICmObject DuplicateDetailObject(DetailField field, Command command)
		{
			SettleDetailEdits();
			var copy = DetailObjectDuplication.Duplicate(RowObjectOf(field), command.UndoText, command.RedoText);
			if (copy == null)
			{
				Logger.WriteEvent("Duplicate did nothing: the row's object " + field?.ObjectHvo + " cannot be duplicated.");
				return null;
			}
			m_avaloniaEntryForm?.FocusObjectOnNextShow(copy.Hvo);
			OnAvaloniaDetailEditCompleted(this, EventArgs.Empty);
			return copy;
		}

		/// <summary>
		/// The Move item for the row's affix slot or affix template: absent on a row with no
		/// object, a ghost row, and a row whose object the command does not act on or no
		/// category owns; enabled otherwise.
		/// </summary>
		internal DetailMenuItem MoveToCategoryItem(DetailMenuLeaf leaf, DetailField field)
		{
			var target = RowObjectOf(field);
			if (target == null || field.GhostPrompt != null || !CommandActsOn(target, leaf.Command)
				|| CategoryItemMove.CategoryOf(target) == null)
			{
				return null;
			}
			var label = XCoreMenuBridge.StripAccelerator(leaf.Label);
			return new DetailMenuItem(label, isEnabled: true, isChecked: false, children: null,
				execute: () => MoveToCategory(field, leaf.Command, label));
		}

		/// <summary>
		/// Moves the row's slot or template to the category the user picks through
		/// <see cref="MoveDestinationChosen"/>: pending edits settle first, the move is one
		/// undoable step, and the re-show focuses the nearest row that is left.
		/// </summary>
		/// <param name="title">The title of the dialog that asks for the category.</param>
		/// <returns>True when the object moved.</returns>
		internal bool MoveToCategory(DetailField field, Command command, string title)
		{
			SettleDetailEdits();
			var target = RowObjectOf(field);
			var current = CategoryItemMove.CategoryOf(target);
			if (current == null)
			{
				Logger.WriteEvent("Move did nothing: no category owns the row's object " + field?.ObjectHvo + ".");
				return false;
			}
			var destination = (MoveDestinationChosen ?? ChooseCategoryWithDialog)(current, title);
			if (destination == null)
				return false;
			var focusRows = RowsToFocusAfterDeleting(m_avaloniaEntryForm?.ShownDetail, target);
			if (!CategoryItemMove.Move(target, destination, command.UndoText, command.RedoText))
				return false;
			if (focusRows.Count > 0)
				m_avaloniaEntryForm?.FocusRowOnNextShow(focusRows);
			OnAvaloniaDetailEditCompleted(this, EventArgs.Empty);
			return true;
		}

		private IPartOfSpeech ChooseCategoryWithDialog(IPartOfSpeech current, string title)
			=> LcmCategoryChooserLauncher.Choose(Cache, m_propertyTable.GetValue<Form>("window"), title);
	}
}
