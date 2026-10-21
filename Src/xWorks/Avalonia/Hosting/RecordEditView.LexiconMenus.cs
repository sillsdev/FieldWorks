// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using SIL.FieldWorks.Common.DetailRules;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FdoUi;
using SIL.FieldWorks.LexText.Controls;
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

		/// <summary>The message every jump leaf (Show ... in ...) of the detail menus
		/// sends.</summary>
		internal const string JumpMessage = "JumpToTool";

		/// <summary>The message of Swap Lexeme Form with Allomorph.</summary>
		internal const string SwapLexemeFormMessage = "SwapLexemeWithAllomorph";

		/// <summary>The message of Convert to Affix Process / Convert to Affix Form.</summary>
		internal const string ConvertLexemeFormMessage = "ConvertLexemeForm";

		/// <summary>The message every Insert leaf of the detail menus sends.</summary>
		internal const string InsertMessage = "DataTreeInsert";

		/// <summary>The message of a leaf that opens a configured dialog, such as Find example
		/// sentence.</summary>
		internal const string LaunchGuiControlMessage = "LaunchGuiControl";

		/// <summary>
		/// Asks the user to confirm a delete: the product shows the object's confirmation
		/// dialog; a headless test answers for the user.
		/// </summary>
		internal Func<CmObjectUi, bool> DeleteConfirmation { get; set; } = ui => ui.ConfirmDelete();

		/// <summary>
		/// Picks the allomorph to swap with the entry's lexeme form, or null to cancel: the
		/// product asks with <see cref="SwapLexemeWithAllomorphDlg"/>; a headless test
		/// answers for the user.
		/// </summary>
		internal Func<ILexEntry, IMoForm> AllomorphToSwap { get; set; }

		/// <summary>
		/// Asks the user to accept the data a form conversion loses, given the warning text:
		/// the product shows it; a headless test answers for the user.
		/// </summary>
		internal Func<string, bool> ConversionLossAccepted { get; set; } = warning =>
			MessageBox.Show(warning, LexemeFormConversion.DataLossCaption, MessageBoxButtons.YesNo,
				MessageBoxIcon.Warning) == DialogResult.Yes;

		/// <summary>
		/// Adds a variant to the entry and returns its entry ref, or null when the user
		/// cancels: the product asks with <see cref="InsertVariantDlg"/>, which links the
		/// variant in its own undoable step; a headless test answers for the user.
		/// </summary>
		internal Func<ILexEntry, ILexEntryRef> VariantToInsert { get; set; }

		/// <summary>
		/// Opens the dialog a command's <c>guicontrol</c> parameter configures, for the
		/// object of the given UI: the product shows it; a headless test records the call.
		/// </summary>
		internal Action<CmObjectUi, Command> GuiControlLaunch { get; set; } = (ui, command) => ui.LaunchGuiControl(command);

		/// <summary>
		/// The message-keyed authority for the Lexicon menus, owning by predicate and
		/// answering for <paramref name="field"/>; with no row every answer is hidden.
		/// </summary>
		internal LexiconMenuAuthority CreateLexiconMenuAuthority(DetailField field)
		{
			var window = m_propertyTable.GetValue<XWindow>("window");
			var authority = new LexiconMenuAuthority(id => XCoreMenuBridge.ResolveMenu(window, id));
			authority.Answer(DeleteMessage, leaf => DeleteItem(leaf, field));
			authority.Answer(JumpMessage, leaf => JumpItem(leaf, field));
			authority.Answer(SwapLexemeFormMessage, leaf => SwapLexemeFormItem(leaf, field));
			authority.Answer(ConvertLexemeFormMessage, leaf => ConvertLexemeFormItem(leaf, field));
			authority.Answer(InsertMessage, leaf => InsertItem(leaf, field), InsertLeafInScope);
			authority.Answer(LaunchGuiControlMessage, leaf => GuiControlItem(leaf, field));
			return authority;
		}

		/// <summary>
		/// The Insert leaves answered natively: a class created in an owning field, or a
		/// variant added through the entry's variant back references. Inserts into
		/// SubPossibilities stay with the adapter.
		/// </summary>
		internal bool InsertLeafInScope(DetailMenuLeaf leaf)
		{
			var command = leaf.Command;
			var fieldName = command.GetParameter("field", null);
			var className = command.GetParameter("className", null);
			if (string.IsNullOrEmpty(fieldName) || string.IsNullOrEmpty(className) || fieldName == "SubPossibilities")
				return false;
			if (!((IFwMetaDataCacheManaged)Cache.MetaDataCacheAccessor).ClassExists(className))
				return false;
			return DetailObjectInsertion.IsOwningFieldName(Cache, fieldName)
				|| DetailObjectInsertion.IsVariantInsert(fieldName, className);
		}

		/// <summary>
		/// The Insert item for the row: absent with no row or no object, and absent when the
		/// entry of the row or the shown entry does not offer an allomorph insert; enabled
		/// when the command resolves a target from the row's object (a ghost row's included),
		/// disabled otherwise. With <c>displayNumber</c> the label carries the position the
		/// shown record's or the row's object's field would give.
		/// </summary>
		internal DetailMenuItem InsertItem(DetailMenuLeaf leaf, DetailField field)
		{
			var rowObject = RowObjectOf(field);
			if (rowObject == null)
				return null;
			var command = leaf.Command;
			var root = ResolveShownRecord(Clerk?.CurrentObject);
			var entry = rowObject as ILexEntry ?? root as ILexEntry;
			if (entry != null && !DetailObjectInsertion.IsOfferedOnEntry(entry, command.Id))
				return null;
			var label = XCoreMenuBridge.StripAccelerator(leaf.Label);
			var fieldName = command.GetParameter("field");
			if (XmlUtils.GetOptionalBooleanAttributeValue(command.Parameters[0], "displayNumber", false)
				&& DetailObjectInsertion.CanInsert(root, rowObject, fieldName, out var index)
				&& index >= 0)
			{
				label += " " + (index + 1);
			}
			var className = command.GetParameter("className");
			if (DetailObjectInsertion.IsVariantInsert(fieldName, className))
			{
				if (DetailObjectInsertion.VariantInsertTarget(rowObject, root, fieldName, className) == null)
					return DetailMenuItem.Disabled(label);
				return new DetailMenuItem(label, isEnabled: true, isChecked: false, children: null,
					execute: () => InsertVariant(field, command));
			}
			if (ResolveInsertTarget(rowObject, command) == null)
				return DetailMenuItem.Disabled(label);
			return new DetailMenuItem(label, isEnabled: true, isChecked: false, children: null,
				execute: () => InsertDetailObject(field, command));
		}

		/// <summary>
		/// Adds a variant to the entry the row's variant insert resolves, through
		/// <see cref="VariantToInsert"/>: pending edits settle first, and the re-show focuses
		/// the variant's Variant Form editor.
		/// </summary>
		/// <returns>The variant's entry ref, or null when nothing was added.</returns>
		internal ILexEntryRef InsertVariant(DetailField field, Command command)
		{
			SettleDetailEdits();
			var entry = DetailObjectInsertion.VariantInsertTarget(RowObjectOf(field), ResolveShownRecord(Clerk?.CurrentObject),
				command.GetParameter("field"), command.GetParameter("className"));
			if (entry == null)
			{
				Logger.WriteEvent("Insert Variant did nothing: neither the row's object nor the shown record is an entry.");
				return null;
			}
			var entryRef = (VariantToInsert ?? InsertVariantWithDialog)(entry);
			if (entryRef == null || !entryRef.IsValidObject)
				return null;
			m_avaloniaEntryForm?.FocusObjectOnNextShow(VariantFocusTarget(entryRef).Hvo);
			OnAvaloniaDetailEditCompleted(this, EventArgs.Empty);
			return entryRef;
		}

		/// <summary>
		/// The object whose row takes focus once a variant is added: the variant's lexeme
		/// form, whose Variant Form row heads the variant's rows; the entry ref when the
		/// variant has no lexeme form.
		/// </summary>
		internal static ICmObject VariantFocusTarget(ILexEntryRef entryRef)
			=> (entryRef.Owner as ILexEntry)?.LexemeFormOA ?? (ICmObject)entryRef;

		private ILexEntryRef InsertVariantWithDialog(ILexEntry entry)
		{
			using (var dlg = new InsertVariantDlg())
			{
				dlg.SetHelpTopic("khtpInsertVariantDlg");
				dlg.SetDlgInfo(Cache, m_mediator, m_propertyTable, entry);
				return dlg.ShowDialog(m_propertyTable.GetValue<Form>("window")) == DialogResult.OK
					? dlg.VariantEntryRefResult
					: null;
			}
		}

		/// <summary>
		/// The item that opens the dialog the command configures (Find example sentence) for
		/// the row's object: absent with no row or no object, enabled otherwise.
		/// </summary>
		internal DetailMenuItem GuiControlItem(DetailMenuLeaf leaf, DetailField field)
		{
			if (RowObjectOf(field) == null)
				return null;
			return new DetailMenuItem(XCoreMenuBridge.StripAccelerator(leaf.Label), isEnabled: true, isChecked: false,
				children: null, execute: () => OpenGuiControl(field, leaf.Command));
		}

		/// <summary>
		/// Opens the command's dialog for the row's object through
		/// <see cref="GuiControlLaunch"/>: pending edits settle first, and the view re-shows
		/// whatever the dialog changed.
		/// </summary>
		internal void OpenGuiControl(DetailField field, Command command)
		{
			SettleDetailEdits();
			var rowObject = RowObjectOf(field);
			if (rowObject == null)
			{
				Logger.WriteEvent("The dialog did not open: the row's object " + field?.ObjectHvo + " no longer exists.");
				return;
			}
			using (var ui = CmObjectUi.MakeUi(rowObject))
			{
				ui.Mediator = m_mediator;
				ui.PropTable = m_propertyTable;
				GuiControlLaunch(ui, command);
			}
			OnAvaloniaDetailEditCompleted(this, EventArgs.Empty);
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
				Logger.WriteEvent(string.Format("Insert did nothing: no owning {0} field that can take a {1} resolves from {2} {3}.",
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

		/// <summary>
		/// The jump item (Show Entry in Concordance and the like) for the row: enabled when
		/// <see cref="DetailJumpTargets"/> resolves a target for the row's object and field,
		/// disabled otherwise. With no row, or a row without an object, the shown record is
		/// the target, as it is for a tree with no current slice.
		/// </summary>
		internal DetailMenuItem JumpItem(DetailMenuLeaf leaf, DetailField field)
		{
			var label = XCoreMenuBridge.StripAccelerator(leaf.Label);
			var tool = XmlUtils.GetMandatoryAttributeValue(leaf.Command.Parameters[0], "tool");
			var className = XmlUtils.GetMandatoryAttributeValue(leaf.Command.Parameters[0], "className");
			var root = ResolveShownRecord(Clerk?.CurrentObject);
			var target = JumpSourceOf(field, root, out var flid);
			var jump = DetailJumpTargets.Resolve(tool, className, leaf.CommandId, Guid.Empty, root, target, flid);
			if (jump.Guid == Guid.Empty)
				return DetailMenuItem.Disabled(label);
			return new DetailMenuItem(label, isEnabled: true, isChecked: false, children: null,
				execute: () => Jump(tool, jump));
		}

		// The object and field a row's jump resolves from: no row or no object means the
		// root with no field; a header row has no field; else the row's object and field.
		private ICmObject JumpSourceOf(DetailField field, ICmObject root, out int? flid)
		{
			flid = null;
			if (field == null || field.ObjectHvo == 0)
				return root;
			if (!Cache.ServiceLocator.ObjectRepository.TryGetObject(field.ObjectHvo, out var target))
				return null;
			if (field.Kind != DetailFieldKind.Header)
				flid = FieldIdOf(target, field.Field);
			return target;
		}

		// The field id of the named field on the object's class, 0 when it has none.
		private int FieldIdOf(ICmObject target, string fieldName)
		{
			if (string.IsNullOrEmpty(fieldName))
				return 0;
			var mdc = (IFwMetaDataCacheManaged)Cache.MetaDataCacheAccessor;
			return mdc.FieldExists(target.ClassID, fieldName, true) ? mdc.GetFieldId2(target.ClassID, fieldName, true) : 0;
		}

		// Follows the resolved jump; a notebook jump from a text without a record creates the
		// record first (its own unit of work, so pending edits settle before it) and shows that.
		private void Jump(string tool, DetailJumpTarget jump)
		{
			SettleDetailEdits();
			var guid = jump.TextWithoutRecord != null
				? DetailJumpTargets.AssociateWithNotebook(jump.TextWithoutRecord).Guid
				: jump.Guid;
			FollowLink(new FwLinkArgs(tool, guid));
		}

		/// <summary>
		/// The Swap Lexeme Form with Allomorph item for the row: absent unless the entry
		/// the row's object belongs to (the shown entry with no row) has a lexeme form and
		/// allomorphs to swap it with.
		/// </summary>
		internal DetailMenuItem SwapLexemeFormItem(DetailMenuLeaf leaf, DetailField field)
		{
			var entry = EntryOf(field);
			if (!LexemeFormConversion.CanSwap(entry))
				return null;
			return new DetailMenuItem(XCoreMenuBridge.StripAccelerator(leaf.Label), isEnabled: true, isChecked: false,
				children: null, execute: () => SwapLexemeForm(entry, leaf.Command));
		}

		/// <summary>
		/// Swaps the entry's lexeme form with the allomorph the user picks: pending edits
		/// settle first, the swap is one undoable step, and the view re-shows.
		/// </summary>
		/// <returns>True when the forms were swapped.</returns>
		internal bool SwapLexemeForm(ILexEntry entry, Command command)
		{
			SettleDetailEdits();
			if (!LexemeFormConversion.CanSwap(entry))
				return false;
			var allomorph = (AllomorphToSwap ?? ChooseAllomorphWithDialog)(entry);
			if (allomorph == null || !entry.AlternateFormsOS.Contains(allomorph))
				return false;
			LexemeFormConversion.Swap(entry, allomorph, command.UndoText, command.RedoText);
			OnAvaloniaDetailEditCompleted(this, EventArgs.Empty);
			return true;
		}

		private IMoForm ChooseAllomorphWithDialog(ILexEntry entry)
		{
			var mainWindow = m_propertyTable.GetValue<Form>("window");
			using (new WaitCursor(mainWindow))
			using (var dlg = new SwapLexemeWithAllomorphDlg())
			{
				dlg.SetDlgInfo(Cache, m_propertyTable, entry);
				return dlg.ShowDialog(mainWindow) == DialogResult.OK ? dlg.SelectedAllomorph : null;
			}
		}

		/// <summary>
		/// The Convert to Affix Process / Convert to Affix Form item for the row: absent
		/// unless the row's form is of the class the command converts from.
		/// </summary>
		internal DetailMenuItem ConvertLexemeFormItem(DetailMenuLeaf leaf, DetailField field)
		{
			var form = FormOf(field);
			var entry = form?.OwnerOfClass<ILexEntry>();
			var toClassId = ClassIdOf(leaf.Command.GetParameter("toClassName"));
			if (entry == null || toClassId == 0
				|| !LexemeFormConversion.CanConvert(form, ClassIdOf(leaf.Command.GetParameter("fromClassName"))))
			{
				return null;
			}
			return new DetailMenuItem(XCoreMenuBridge.StripAccelerator(leaf.Label), isEnabled: true, isChecked: false,
				children: null, execute: () => ConvertForm(entry, form, toClassId, leaf.Command));
		}

		/// <summary>
		/// Converts the form to <paramref name="toClassId"/> once the user accepts what the
		/// conversion loses: pending edits settle first, the conversion is one undoable step,
		/// and the re-show focuses the new form's row.
		/// </summary>
		/// <returns>True when the form was converted.</returns>
		internal bool ConvertForm(ILexEntry entry, IMoForm form, int toClassId, Command command)
		{
			SettleDetailEdits();
			if (!form.IsValidObject || form.Owner != entry)
				return false;
			var warning = LexemeFormConversion.DataLossMessage(form);
			if (warning != null && !ConversionLossAccepted(warning))
				return false;
			var newForm = LexemeFormConversion.Convert(entry, form, toClassId, command.UndoText, command.RedoText);
			m_avaloniaEntryForm?.FocusObjectOnNextShow(newForm.Hvo);
			OnAvaloniaDetailEditCompleted(this, EventArgs.Empty);
			return true;
		}

		// The row's object as a form, null when it is none or gone.
		private IMoForm FormOf(DetailField field)
		{
			if (field == null || field.ObjectHvo == 0 || field.GhostPrompt != null)
				return null;
			return Cache.ServiceLocator.ObjectRepository.TryGetObject(field.ObjectHvo, out var target) ? target as IMoForm : null;
		}

		// The entry the row's object belongs to, or the shown entry when the row shows none.
		private ILexEntry EntryOf(DetailField field)
		{
			if (field != null && field.ObjectHvo != 0
				&& Cache.ServiceLocator.ObjectRepository.TryGetObject(field.ObjectHvo, out var target))
			{
				return target as ILexEntry ?? target.OwnerOfClass<ILexEntry>();
			}
			return ResolveShownRecord(Clerk?.CurrentObject) as ILexEntry;
		}

		private int ClassIdOf(string className)
		{
			var mdc = (IFwMetaDataCacheManaged)Cache.MetaDataCacheAccessor;
			return !string.IsNullOrEmpty(className) && mdc.ClassExists(className) ? mdc.GetClassId(className) : 0;
		}
	}
}
