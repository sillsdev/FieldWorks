// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using SIL.FieldWorks.Common.DetailRules;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.FieldWorks.FdoUi;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;
using SIL.Reporting;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// The host half of the Lexicon detail menus: the answerers given to
	/// <see cref="LexiconMenuAuthority"/> and the commands they run.
	/// </summary>
	public partial class RecordEditView
	{
		/// <summary>The message every Delete leaf of the detail menus sends.</summary>
		internal const string DeleteMessage = "DataTreeDelete";

		/// <summary>The message every Insert leaf of the detail menus sends.</summary>
		internal const string InsertMessage = "DataTreeInsert";

		// The command-id suffixes of the allomorph inserts.
		private static readonly string[] AllomorphInsertSuffixes = { "AffixProcess", "AffixAllomorph", "StemAllomorph" };

		/// <summary>
		/// Asks the user to confirm a delete: the product shows the object's confirmation
		/// dialog; a headless test answers for the user.
		/// </summary>
		internal Func<CmObjectUi, bool> DeleteConfirmation { get; set; } = ui => ui.ConfirmDelete();

		/// <summary>
		/// The message-keyed authority for the Lexicon menus, owning by predicate and
		/// answering for <paramref name="field"/>; with no row every answer is hidden.
		/// </summary>
		internal LexiconMenuAuthority CreateLexiconMenuAuthority(DetailField field)
		{
			var window = m_propertyTable.GetValue<XWindow>("window");
			var authority = new LexiconMenuAuthority(id => XCoreMenuBridge.ResolveMenu(window, id));
			authority.Answer(DeleteMessage, leaf => DeleteItem(leaf, field));
			authority.Answer(InsertMessage, leaf => InsertItem(leaf, field), InsertLeafInScope);
			return authority;
		}

		/// <summary>
		/// The Insert leaves answered natively: a concrete class created in an owning field.
		/// </summary>
		internal bool InsertLeafInScope(DetailMenuLeaf leaf)
		{
			var command = leaf.Command;
			var fieldName = command.GetParameter("field", null);
			var className = command.GetParameter("className", null);
			if (string.IsNullOrEmpty(fieldName) || string.IsNullOrEmpty(className) || fieldName == "SubPossibilities")
				return false;
			foreach (var suffix in AllomorphInsertSuffixes)
			{
				if (command.Id.EndsWith(suffix, StringComparison.Ordinal))
					return false;
			}
			var mdc = (IFwMetaDataCacheManaged)Cache.MetaDataCacheAccessor;
			if (!mdc.ClassExists(className) || mdc.GetAbstract(mdc.GetClassId(className)))
				return false;
			return DetailObjectInsertion.IsOwningFieldName(Cache, fieldName);
		}

		/// <summary>
		/// The Insert item for the row: absent with no row or no object; enabled when the
		/// command resolves an owning target from the row's object (a ghost row's included),
		/// disabled otherwise. With <c>displayNumber</c> the label carries the position the
		/// shown record's or the row's object's field would give.
		/// </summary>
		internal DetailMenuItem InsertItem(DetailMenuLeaf leaf, DetailField field)
		{
			var rowObject = RowObjectOf(field);
			if (rowObject == null)
				return null;
			var label = XCoreMenuBridge.StripAccelerator(leaf.Label);
			var command = leaf.Command;
			var fieldName = command.GetParameter("field");
			if (XmlUtils.GetOptionalBooleanAttributeValue(command.Parameters[0], "displayNumber", false)
				&& DetailObjectInsertion.CanInsert(ResolveShownRecord(Clerk?.CurrentObject), rowObject, fieldName, out var index)
				&& index >= 0)
			{
				label += " " + (index + 1);
			}
			if (ResolveInsertTarget(rowObject, command) == null)
				return DetailMenuItem.Disabled(label);
			return new DetailMenuItem(label, isEnabled: true, isChecked: false, children: null,
				execute: () => InsertDetailObject(field, command));
		}

		// The command's target from the row's object, by its field, className, ownerClass and
		// slice parameters; null when none resolves.
		private static DetailInsertTarget ResolveInsertTarget(ICmObject rowObject, Command command)
			=> DetailObjectInsertion.ResolveTarget(rowObject, command.GetParameter("field"), command.GetParameter("className"),
				command.GetParameter("ownerClass", null), command.GetParameter("slice", null) == "owner");

		/// <summary>
		/// Inserts the object the row's Insert command creates, where
		/// <see cref="DetailObjectInsertion.ResolveTarget"/> puts it: pending edits settle
		/// first, the object's UI creates it in one undoable step, and the re-show focuses the
		/// new object's first row.
		/// </summary>
		/// <returns>The new object, or null when nothing was inserted.</returns>
		internal ICmObject InsertDetailObject(DetailField field, Command command)
		{
			SettleDetailEdits();
			var rowObject = RowObjectOf(field);
			if (rowObject == null)
			{
				Logger.WriteEvent("Insert did nothing: the row's object " + field?.ObjectHvo + " no longer exists.");
				return null;
			}
			var target = ResolveInsertTarget(rowObject, command);
			if (target == null)
			{
				Logger.WriteEvent(string.Format("Insert did nothing: no owning {0} field for a {1} resolves from {2} {3}.",
					command.GetParameter("field"), command.GetParameter("className"), rowObject.ClassName, rowObject.Hvo));
				return null;
			}
			ICmObject created;
			using (var ui = CmObjectUi.CreateNewUiObject(m_mediator, m_propertyTable, target.ClassId, target.Owner.Hvo,
				target.Flid, target.Position))
			{
				created = ui?.Object;
			}
			if (created == null)
				return null;
			m_avaloniaEntryForm?.FocusObjectOnNextShow(created.Hvo);
			OnAvaloniaDetailEditCompleted(this, EventArgs.Empty);
			return created;
		}

		// The row's object, null when the row has none or it is gone. A ghost row keeps the
		// object it stands in for.
		private ICmObject RowObjectOf(DetailField field)
		{
			if (field == null || field.ObjectHvo == 0)
				return null;
			return Cache.ServiceLocator.ObjectRepository.TryGetObject(field.ObjectHvo, out var target) ? target : null;
		}

		/// <summary>
		/// The Delete item for the row's object: absent on a row with no object or a ghost
		/// row; disabled when the object is gone from the repository or
		/// <see cref="DetailObjectDeletion.CanDeleteNow"/> refuses it.
		/// </summary>
		internal DetailMenuItem DeleteItem(DetailMenuLeaf leaf, DetailField field)
		{
			if (field == null || field.ObjectHvo == 0 || field.GhostPrompt != null)
				return null;
			var label = XCoreMenuBridge.StripAccelerator(leaf.Label);
			if (!Cache.ServiceLocator.ObjectRepository.TryGetObject(field.ObjectHvo, out var target))
			{
				Logger.WriteEvent("Delete is disabled: the row's object " + field.ObjectHvo + " no longer exists.");
				return DetailMenuItem.Disabled(DetailObjectDeletion.DeleteLabel(label, null, false));
			}
			var canDelete = DetailObjectDeletion.CanDeleteNow(target);
			var text = DetailObjectDeletion.DeleteLabel(label, target, canDelete);
			return new DetailMenuItem(text, isEnabled: canDelete, isChecked: false, children: null,
				execute: canDelete ? (Action)(() => DeleteDetailObject(field)) : null);
		}

		/// <summary>
		/// Deletes the row's object after confirmation: pending edits settle first, the
		/// object's UI runs the delete, and the re-show focuses the nearest surviving row, the
		/// one before the deleted object's rows when it survives.
		/// </summary>
		/// <returns>True when the object was deleted.</returns>
		internal bool DeleteDetailObject(DetailField field)
		{
			SettleDetailEdits();
			if (!Cache.ServiceLocator.ObjectRepository.TryGetObject(field.ObjectHvo, out var target))
			{
				Logger.WriteEvent("Delete did nothing: the row's object " + field.ObjectHvo + " no longer exists.");
				return false;
			}
			var focusRows = RowsToFocusAfterDeleting(m_avaloniaEntryForm?.ShownDetail, target);
			bool deleted;
			using (var ui = CmObjectUi.MakeUi(Cache, target.Hvo))
			{
				ui.Mediator = m_mediator;
				ui.PropTable = m_propertyTable;
				deleted = ui.DeleteUnderlyingObject(() => DeleteConfirmation(ui));
			}
			if (!deleted)
				return false;
			if (focusRows.Count > 0)
				m_avaloniaEntryForm?.FocusRowOnNextShow(focusRows);
			OnAvaloniaDetailEditCompleted(this, EventArgs.Empty);
			return true;
		}

		/// <summary>
		/// The rows to try focusing once <paramref name="target"/> is deleted, by automation
		/// id: the rows before its rows (its own and those of the objects it owns), nearest
		/// first, then the rows after them, nearest first. The re-show takes the first that
		/// still exists; a row can vanish with the object, e.g. a section header whose
		/// sequence it emptied.
		/// </summary>
		internal static IReadOnlyList<string> RowsToFocusAfterDeleting(DetailModel shown, ICmObject target)
		{
			var candidates = new List<string>();
			if (shown == null || target == null)
				return candidates;
			var fields = shown.Fields;
			var first = -1;
			var last = -1;
			for (var i = 0; i < fields.Count; i++)
			{
				if (!ShowsPartOf(fields[i], target))
					continue;
				if (first < 0)
					first = i;
				last = i;
			}
			if (first < 0)
				return candidates;
			for (var i = first - 1; i >= 0; i--)
				candidates.Add(fields[i].RowAutomationId);
			for (var i = last + 1; i < fields.Count; i++)
				candidates.Add(fields[i].RowAutomationId);
			return candidates;
		}

		// Whether the row shows the object or one it owns, directly or through other objects.
		private static bool ShowsPartOf(DetailField field, ICmObject target)
		{
			if (field.ObjectHvo == 0
				|| !target.Cache.ServiceLocator.ObjectRepository.TryGetObject(field.ObjectHvo, out var shown))
			{
				return false;
			}
			for (var owned = shown; owned != null; owned = owned.Owner)
			{
				if (owned.Hvo == target.Hvo)
					return true;
			}
			return false;
		}
	}
}
