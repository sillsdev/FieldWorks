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
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FdoUi;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.FieldWorks.LexText.Controls;
using SIL.FieldWorks.Resources;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
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

		/// <summary>The message every Move ... Up leaf of the detail menus sends.</summary>
		internal const string MoveUpMessage = "MoveUpObjectInSequence";

		/// <summary>The message every Move ... Down leaf of the detail menus sends.</summary>
		internal const string MoveDownMessage = "MoveDownObjectInSequence";

		/// <summary>The message of Insert Sound or Movie.</summary>
		internal const string InsertMediaFileMessage = "InsertMediaFile";

		/// <summary>The message of Picture Properties.</summary>
		internal const string PicturePropertiesMessage = "PictureProperties";

		/// <summary>The message every Merge ... into leaf of the detail menus sends.</summary>
		internal const string MergeMessage = "DataTreeMerge";

		/// <summary>The message of Delete Reference on a variant's row.</summary>
		internal const string DeleteReferenceMessage = "DataTreeDeleteReference";

		/// <summary>The message of Add Reference and Replace Reference on a lexical-relation
		/// row.</summary>
		internal const string AddReferenceMessage = "DataTreeAddReference";

		/// <summary>The message of Edit Reference Set Details.</summary>
		internal const string EditReferenceMessage = "DataTreeEdit";

		/// <summary>The message of Swap Allomorph with Lexeme Form.</summary>
		internal const string SwapAllomorphMessage = "SwapAllomorphWithLexeme";

		/// <summary>The message of Convert to Affix Process / Convert to Affix Allomorph on an
		/// allomorph's row.</summary>
		internal const string ConvertAllomorphMessage = "ConvertAllomorph";

		/// <summary>
		/// Asks the user to confirm a delete: the product shows the object's confirmation
		/// dialog; a headless test answers for the user.
		/// </summary>
		internal Func<CmObjectUi, bool> DeleteConfirmation { get; set; } = ui => ui.ConfirmDelete();

		/// <summary>
		/// Asks the user to confirm deleting a relation, with the note on what else the
		/// delete affects (null for none): the product shows the relation's confirmation
		/// dialog; a headless test answers for the user.
		/// </summary>
		internal Func<CmObjectUi, ITsString, bool> RelationDeleteConfirmation { get; set; } = (ui, note) => ui.ConfirmDelete(note);

		/// <summary>
		/// Picks the survivor of a merge among the sorted candidates, by hvo, or 0 to cancel:
		/// the product asks with <see cref="MergeObjectDlg"/> (null here); a headless test
		/// answers for the user.
		/// </summary>
		internal Func<IReadOnlyList<DummyCmObject>, int> MergeSurvivor { get; set; }

		/// <summary>
		/// Picks the entry or sense to add to a lexical relation or put in place of its
		/// reference, or null to cancel: the product asks with the chooser the request
		/// describes; a headless test answers for the user.
		/// </summary>
		internal Func<LexicalRelationChooser, ICmObject> RelationTargetChosen { get; set; }

		/// <summary>
		/// Edits a relation's name and comment, or returns null to cancel: the product asks
		/// with <see cref="LexReferenceDetailsDlg"/>; a headless test answers for the user.
		/// </summary>
		internal Func<ILexReference, RelationDetails> RelationDetailsEdited { get; set; }

		/// <summary>The name and comment Edit Reference Set Details gives a relation.</summary>
		internal sealed class RelationDetails
		{
			public string Name { get; set; }
			public string Comment { get; set; }
		}

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
		/// Picks the sound or movie files to link, by their names under the linked-files
		/// folder, or an empty list to cancel: the product asks with the file dialog and
		/// offers to move or copy the files there; a headless test answers for the user.
		/// </summary>
		internal Func<IReadOnlyList<string>> MediaFilesToInsert { get; set; }

		/// <summary>
		/// Picks the picture's new file, or null to leave it: the product asks with
		/// <see cref="PicturePropertiesDialog"/>; a headless test answers for the user.
		/// </summary>
		internal Func<ICmPicture, string> PictureFileChosen { get; set; }

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
			authority.Answer(MoveUpMessage, leaf => MoveItem(leaf, field, up: true));
			authority.Answer(MoveDownMessage, leaf => MoveItem(leaf, field, up: false));
			authority.Answer(InsertMediaFileMessage, leaf => InsertMediaItem(leaf, field));
			authority.Answer(PicturePropertiesMessage, leaf => PicturePropertiesItem(leaf, field));
			authority.Answer(MergeMessage, leaf => MergeItem(leaf, field));
			authority.Answer(DeleteReferenceMessage, leaf => DeleteReferenceItem(leaf, field));
			authority.Answer(AddReferenceMessage, leaf => AddReferenceItem(leaf, field));
			authority.Answer(EditReferenceMessage, leaf => EditReferenceItem(leaf, field));
			authority.Answer(SwapAllomorphMessage, leaf => SwapAllomorphItem(leaf, field));
			authority.Answer(ConvertAllomorphMessage, leaf => ConvertLexemeFormItem(leaf, field));
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
		/// entry of the row or the shown entry does not offer the insert; enabled
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

		// The object a row's Delete or Merge acts on: the variant entry on a variant's rows,
		// else the row's object.
		private ICmObject CommandTargetOf(ICmObject rowObject)
			=> VariantBackReferences.CommandTarget(ResolveShownRecord(Clerk?.CurrentObject), rowObject);

		/// <summary>
		/// The Delete item for the row's object (the variant entry on a variant's row): absent
		/// on a row with no object, a ghost row and a row whose object the command does not act
		/// on; disabled when the object is gone from the repository or
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
			target = CommandTargetOf(target);
			if (!CommandActsOn(target, leaf.Command))
				return null;
			var canDelete = DetailObjectDeletion.CanDeleteNow(target);
			var text = DetailObjectDeletion.DeleteLabel(label, target, canDelete);
			return new DetailMenuItem(text, isEnabled: canDelete, isChecked: false, children: null,
				execute: canDelete ? (Action)(() => DeleteDetailObject(field)) : null);
		}

		/// <summary>
		/// Deletes the row's object (the variant entry on a variant's row) after confirmation:
		/// pending edits settle first, the object's UI runs the delete, and the re-show focuses
		/// the nearest surviving row, the one before the deleted object's rows when it survives.
		/// A relation row runs <see cref="DeleteRelation"/> instead.
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
			if (target is ILexReference relation)
				return DeleteRelation(field, relation);
			target = CommandTargetOf(target);
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

		/// <summary>
		/// Whether a Delete or Move command acts on <paramref name="target"/>: its
		/// <c>className</c> is the target's class, or the class of the owner whose
		/// <c>field</c> holds the target (the picture commands name the sense). A command
		/// naming no class, or one the model lacks, acts on any object. This keeps the items
		/// off a section row, whose object is the sequence's owner.
		/// </summary>
		internal static bool CommandActsOn(ICmObject target, Command command)
		{
			var className = command.GetParameter("className", null);
			if (string.IsNullOrEmpty(className))
				return true;
			var cache = target.Cache;
			var mdc = (IFwMetaDataCacheManaged)cache.MetaDataCacheAccessor;
			if (!mdc.ClassExists(className))
				return true;
			var classId = mdc.GetClassId(className);
			if (cache.ClassIsOrInheritsFrom(target.ClassID, classId))
				return true;
			var owner = target.Owner;
			var fieldName = command.GetParameter("field", null);
			return owner != null && !string.IsNullOrEmpty(fieldName) && cache.ClassIsOrInheritsFrom(owner.ClassID, classId)
				&& mdc.GetFieldName(target.OwningFlid) == fieldName;
		}

		/// <summary>
		/// The Move Up or Move Down item for the row's object: absent on a row with no
		/// object, a ghost row, a row whose object the command does not act on, and an object
		/// outside an owning sequence; disabled at the end it would move past.
		/// </summary>
		/// <param name="up">True for Move Up, false for Move Down.</param>
		internal DetailMenuItem MoveItem(DetailMenuLeaf leaf, DetailField field, bool up)
		{
			var target = RowObjectOf(field);
			if (target == null || field.GhostPrompt != null || !CommandActsOn(target, leaf.Command)
				|| !DetailSequenceMove.IsInSequence(target))
			{
				return null;
			}
			var label = XCoreMenuBridge.StripAccelerator(leaf.Label);
			if (!(up ? DetailSequenceMove.CanMoveUp(target) : DetailSequenceMove.CanMoveDown(target)))
				return DetailMenuItem.Disabled(label);
			return new DetailMenuItem(label, isEnabled: true, isChecked: false, children: null,
				execute: () => MoveDetailObject(field, up));
		}

		/// <summary>
		/// Moves the row's object one place up or down in its owning sequence: pending edits
		/// settle first, the move is one undoable step, and the re-show keeps the keyboard on
		/// the moved object's rows.
		/// </summary>
		/// <returns>True when the object moved.</returns>
		internal bool MoveDetailObject(DetailField field, bool up)
		{
			SettleDetailEdits();
			var target = RowObjectOf(field);
			if (target == null)
			{
				Logger.WriteEvent("Move did nothing: the row's object " + field?.ObjectHvo + " no longer exists.");
				return false;
			}
			var moved = up
				? DetailSequenceMove.MoveUp(target, xWorksStrings.UndoMoveItem, xWorksStrings.RedoMoveItem)
				: DetailSequenceMove.MoveDown(target, xWorksStrings.UndoMoveItem, xWorksStrings.RedoMoveItem);
			if (!moved)
				return false;
			m_avaloniaEntryForm?.FocusObjectOnNextShow(target.Hvo);
			OnAvaloniaDetailEditCompleted(this, EventArgs.Empty);
			return true;
		}

		/// <summary>
		/// The Insert Sound or Movie item for the row: absent with no row or no object, and
		/// when neither the row's object nor the shown record belongs to an entry; enabled
		/// otherwise. The files go to the row's pronunciation, else the entry's first, created
		/// when it has none.
		/// </summary>
		internal DetailMenuItem InsertMediaItem(DetailMenuLeaf leaf, DetailField field)
		{
			var rowObject = RowObjectOf(field);
			if (rowObject == null || MediaFileInsertion.TargetEntry(rowObject, ResolveShownRecord(Clerk?.CurrentObject)) == null)
				return null;
			return new DetailMenuItem(XCoreMenuBridge.StripAccelerator(leaf.Label), isEnabled: true, isChecked: false,
				children: null, execute: () => InsertMediaFiles(field));
		}

		/// <summary>
		/// Links the files the user picks to the row's pronunciation, else to the entry's
		/// first, created once the user has picked: pending edits settle first, the
		/// pronunciation and its links are one undoable step, and the re-show focuses the
		/// pronunciation.
		/// </summary>
		/// <returns>The media objects created.</returns>
		internal IReadOnlyList<ICmMedia> InsertMediaFiles(DetailField field)
		{
			SettleDetailEdits();
			var root = ResolveShownRecord(Clerk?.CurrentObject);
			var rowObject = RowObjectOf(field);
			var entry = MediaFileInsertion.TargetEntry(rowObject, root);
			if (entry == null)
			{
				Logger.WriteEvent("Insert Sound or Movie did nothing: no entry for the row's object " + field?.ObjectHvo + ".");
				return new ICmMedia[0];
			}
			var files = (MediaFilesToInsert ?? ChooseMediaFilesWithDialog)();
			if (files == null || files.Count == 0)
				return new ICmMedia[0];
			ILexPronunciation pronunciation = null;
			IReadOnlyList<ICmMedia> created = null;
			UndoableUnitOfWorkHelper.Do(xWorksStrings.ksUndoInsertMedia, xWorksStrings.ksRedoInsertMedia, Cache.ActionHandlerAccessor, () =>
			{
				pronunciation = MediaFileInsertion.TargetPronunciation(rowObject, root)
					?? MediaFileInsertion.FirstOrCreatePronunciation(entry, xWorksStrings.ksUndoCreatePronunciation,
						xWorksStrings.ksRedoCreatePronunciation);
				created = MediaFileInsertion.Insert(pronunciation, files, xWorksStrings.ksUndoInsertMedia, xWorksStrings.ksRedoInsertMedia);
			});
			m_avaloniaEntryForm?.FocusObjectOnNextShow(pronunciation.Hvo);
			OnAvaloniaDetailEditCompleted(this, EventArgs.Empty);
			return created;
		}

		private IReadOnlyList<string> ChooseMediaFilesWithDialog()
			=> DTMenuHandler.ChooseMediaFiles(Cache, m_propertyTable,
				ResourceHelper.BuildFileFilter(FileFilterType.AllAudio, FileFilterType.AllVideo, FileFilterType.AllFiles),
				xWorksStrings.ChooseSoundOrMovieFile);

		/// <summary>
		/// The Picture Properties item for the row: absent unless the row's object is a
		/// picture, enabled then.
		/// </summary>
		internal DetailMenuItem PicturePropertiesItem(DetailMenuLeaf leaf, DetailField field)
		{
			if (!(RowObjectOf(field) is ICmPicture))
				return null;
			return new DetailMenuItem(XCoreMenuBridge.StripAccelerator(leaf.Label), isEnabled: true, isChecked: false,
				children: null, execute: () => EditPictureProperties(field));
		}

		/// <summary>
		/// Points the row's picture at the file the user picks: pending edits settle first,
		/// the update is one undoable step, and the view re-shows.
		/// </summary>
		/// <returns>True when the picture changed.</returns>
		internal bool EditPictureProperties(DetailField field)
		{
			SettleDetailEdits();
			if (!(RowObjectOf(field) is ICmPicture picture))
			{
				Logger.WriteEvent("Picture Properties did nothing: the row's object " + field?.ObjectHvo + " is no picture.");
				return false;
			}
			var file = (PictureFileChosen ?? ChoosePictureFileWithDialog)(picture);
			if (string.IsNullOrEmpty(file))
				return false;
			UndoableUnitOfWorkHelper.Do(xWorksStrings.ksUndoUpdatePicture, xWorksStrings.ksRedoUpdatePicture, picture,
				() => picture.UpdatePicture(file, null, CmFolderTags.DefaultPictureFolder, 0));
			OnAvaloniaDetailEditCompleted(this, EventArgs.Empty);
			return true;
		}

		private string ChoosePictureFileWithDialog(ICmPicture picture)
		{
			using (var dlg = new PicturePropertiesDialog(Cache, picture, m_propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider"),
				m_propertyTable.GetValue<IApp>("App")))
			{
				dlg.Initialize();
				return dlg.ShowDialog(m_propertyTable.GetValue<Form>("window")) == DialogResult.OK ? dlg.CurrentFile : null;
			}
		}

		/// <summary>
		/// The Merge item for the row's object (the variant entry on a variant's row): absent
		/// on a row with no object, a ghost row and a row whose object the command does not act
		/// on; disabled when <see cref="DetailObjectMerge.CanMergeNow"/> refuses it.
		/// </summary>
		internal DetailMenuItem MergeItem(DetailMenuLeaf leaf, DetailField field)
		{
			var rowObject = RowObjectOf(field);
			if (rowObject == null || field.GhostPrompt != null)
				return null;
			var target = CommandTargetOf(rowObject);
			if (!CommandActsOn(target, leaf.Command))
				return null;
			var canMerge = DetailObjectMerge.CanMergeNow(target);
			var label = DetailObjectMerge.MergeLabel(XCoreMenuBridge.StripAccelerator(leaf.Label), canMerge);
			return new DetailMenuItem(label, isEnabled: canMerge, isChecked: false, children: null,
				execute: canMerge ? (Action)(() => MergeDetailObject(field)) : null);
		}

		/// <summary>
		/// Merges the row's object into the survivor the user picks through
		/// <see cref="MergeSurvivor"/>: pending edits settle first, the object's UI runs the
		/// merge in one undoable step, and the re-show focuses the survivor.
		/// </summary>
		/// <returns>True when the objects were merged.</returns>
		internal bool MergeDetailObject(DetailField field)
		{
			SettleDetailEdits();
			var target = CommandTargetOf(RowObjectOf(field));
			if (target == null)
			{
				Logger.WriteEvent("Merge did nothing: the row's object " + field?.ObjectHvo + " no longer exists.");
				return false;
			}
			if (!DetailObjectMerge.CanMergeNow(target))
				return false;
			int survivor;
			using (var ui = CmObjectUi.MakeUi(Cache, target.Hvo))
			{
				ui.Mediator = m_mediator;
				ui.PropTable = m_propertyTable;
				survivor = ui.MergeUnderlyingObject(true, MergeSurvivor);
			}
			if (survivor == 0)
				return false;
			m_avaloniaEntryForm?.FocusObjectOnNextShow(survivor);
			OnAvaloniaDetailEditCompleted(this, EventArgs.Empty);
			return true;
		}

		/// <summary>
		/// The Delete Reference item for a variant's row: absent with no row, no object or a
		/// ghost row; enabled when the row's variant has a ref to the shown entry, disabled
		/// (with the cannot-delete note) on any other row.
		/// </summary>
		internal DetailMenuItem DeleteReferenceItem(DetailMenuLeaf leaf, DetailField field)
		{
			var rowObject = RowObjectOf(field);
			if (rowObject == null || field.GhostPrompt != null)
				return null;
			var label = XCoreMenuBridge.StripAccelerator(leaf.Label);
			if (VariantReferenceOf(field, rowObject) == null)
				return DetailMenuItem.Disabled(label + StringTable.Table.GetString(DetailObjectDeletion.CannotDeleteKey));
			return new DetailMenuItem(label, isEnabled: true, isChecked: false, children: null,
				execute: () => DeleteVariantReference(field));
		}

		/// <summary>
		/// Unlinks the row's variant from the shown entry: pending edits settle first, the
		/// ref goes in one undoable step, and the re-show focuses the nearest surviving row.
		/// </summary>
		/// <returns>True when the ref was deleted.</returns>
		internal bool DeleteVariantReference(DetailField field)
		{
			SettleDetailEdits();
			var rowObject = RowObjectOf(field);
			var reference = rowObject == null ? null : VariantReferenceOf(field, rowObject);
			if (!(ResolveShownRecord(Clerk?.CurrentObject) is ILexEntry root) || reference == null)
			{
				Logger.WriteEvent("Delete Reference did nothing: no variant ref to the shown entry on row " + field?.StableId + ".");
				return false;
			}
			var focusRows = RowsToFocusAfterDeleting(m_avaloniaEntryForm?.ShownDetail, reference.OwningEntry);
			VariantBackReferences.DeleteReference(root, reference);
			if (focusRows.Count > 0)
				m_avaloniaEntryForm?.FocusRowOnNextShow(focusRows);
			OnAvaloniaDetailEditCompleted(this, EventArgs.Empty);
			return true;
		}

		// The ref linking the row's variant to the shown entry: the only one, or among
		// several the one whose own rows follow the row in the shown model, as a variant's
		// Variant Info rows follow its form.
		private ILexEntryRef VariantReferenceOf(DetailField field, ICmObject rowObject)
		{
			var root = ResolveShownRecord(Clerk?.CurrentObject) as ILexEntry;
			var variant = VariantBackReferences.BackReferencedEntry(root, rowObject);
			if (variant == null)
				return null;
			var references = VariantBackReferences.ReferencesTo(root, variant);
			if (references.Count == 0)
				return null;
			var fields = m_avaloniaEntryForm?.ShownDetail?.Fields;
			if (references.Count > 1 && fields != null)
			{
				var index = IndexOfRow(fields, field);
				for (var i = index + 1; index >= 0 && i < fields.Count; i++)
				{
					foreach (var reference in references)
					{
						if (reference.Hvo == fields[i].ObjectHvo)
							return reference;
					}
				}
			}
			return references[0];
		}

		// The row's index among the shown fields: the row itself, else the first row with its
		// id (a variant linked twice shows its Form row twice under one id).
		private static int IndexOfRow(IReadOnlyList<DetailField> fields, DetailField field)
		{
			var byId = -1;
			for (var i = 0; i < fields.Count; i++)
			{
				if (ReferenceEquals(fields[i], field))
					return i;
				if (byId < 0 && fields[i].StableId == field.StableId)
					byId = i;
			}
			return byId;
		}

		// The entry or sense a lexical-relation row belongs to, from its id; null for any
		// other row or when it is gone.
		private ICmObject LexicalRelationOwnerOf(DetailField field)
		{
			if (field == null || !DetailComposer.TryGetLexicalRelationOwner(field.StableId, out var hvo))
				return null;
			return Cache.ServiceLocator.ObjectRepository.TryGetObject(hvo, out var owner) ? owner : null;
		}

		/// <summary>
		/// Runs Delete Relation for the row's relation: after confirmation with the note on
		/// what else it affects, the entry or sense leaves the relation or the relation goes
		/// (<see cref="LexicalRelationEditing.DeleteKind"/>), in one undoable step, and the
		/// re-show focuses the nearest surviving row.
		/// </summary>
		/// <returns>True when the relation changed.</returns>
		internal bool DeleteRelation(DetailField field, ILexReference relation)
		{
			var current = LexicalRelationOwnerOf(field);
			if (current == null)
			{
				Logger.WriteEvent("Delete Relation did nothing: row " + field?.StableId + " names no entry or sense.");
				return false;
			}
			var focusRows = RowsToFocusAfterDeleting(m_avaloniaEntryForm?.ShownDetail, relation);
			bool confirmed;
			using (var ui = CmObjectUi.MakeUi(Cache, relation.Hvo))
			{
				ui.Mediator = m_mediator;
				ui.PropTable = m_propertyTable;
				confirmed = RelationDeleteConfirmation(ui, LexicalRelationEditing.DeleteNote(relation, current));
			}
			if (!confirmed)
				return false;
			LexicalRelationEditing.Delete(relation, current);
			if (focusRows.Count > 0)
				m_avaloniaEntryForm?.FocusRowOnNextShow(focusRows);
			OnAvaloniaDetailEditCompleted(this, EventArgs.Empty);
			return true;
		}

		/// <summary>
		/// The Add Reference / Replace Reference item for a lexical-relation row: absent
		/// unless the row's object is a relation of a known entry or sense; enabled then.
		/// </summary>
		internal DetailMenuItem AddReferenceItem(DetailMenuLeaf leaf, DetailField field)
		{
			if (!(RowObjectOf(field) is ILexReference) || LexicalRelationOwnerOf(field) == null)
				return null;
			return new DetailMenuItem(XCoreMenuBridge.StripAccelerator(leaf.Label), isEnabled: true, isChecked: false,
				children: null, execute: () => AddOrReplaceReference(field));
		}

		/// <summary>
		/// Adds the entry or sense the user picks through <see cref="RelationTargetChosen"/>
		/// to the row's relation, or puts it in place of the row's reference
		/// (<see cref="LexicalRelationEditing.AddOrReplace"/>): pending edits settle first,
		/// the change is one undoable step, and the re-show focuses the relation's row.
		/// </summary>
		/// <returns>The relation the row's entry or sense is now in; null when nothing
		/// changed.</returns>
		internal ILexReference AddOrReplaceReference(DetailField field)
		{
			SettleDetailEdits();
			var relation = RowObjectOf(field) as ILexReference;
			var current = LexicalRelationOwnerOf(field);
			if (relation == null || current == null)
			{
				Logger.WriteEvent("Add Reference did nothing: row " + field?.StableId + " names no relation of an entry or sense.");
				return null;
			}
			var chooser = LexicalRelationEditing.Chooser(relation, current);
			var chosen = (RelationTargetChosen ?? ChooseRelationTargetWithDialog)(chooser);
			if (chosen == null)
				return null;
			var result = LexicalRelationEditing.AddOrReplace(relation, current, chosen);
			if (result == null)
				return null;
			m_avaloniaEntryForm?.FocusObjectOnNextShow(result.Hvo);
			OnAvaloniaDetailEditCompleted(this, EventArgs.Empty);
			return result;
		}

		private ICmObject ChooseRelationTargetWithDialog(LexicalRelationChooser chooser)
		{
			var mainWindow = m_propertyTable.GetValue<Form>("window");
			if (chooser.UseLinkDialog)
			{
				return LcmLinkEntryOrSenseDialogLauncher.Show(Cache, m_mediator, m_propertyTable, null, mainWindow,
					LexicalRelationEditing.ChooserHelpTopic, m_propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider", null),
					allowSenses: chooser.AllowSenses, sensesOnly: chooser.SensesOnly, title: chooser.Title,
					okButtonText: chooser.ButtonText);
			}
			using (var dlg = new EntryGoDlg())
			{
				dlg.SetDlgInfo(Cache, new WindowParams { m_title = chooser.Title, m_btnText = chooser.ButtonText }, m_mediator,
					m_propertyTable);
				dlg.SetHelpTopic(LexicalRelationEditing.ChooserHelpTopic);
				return dlg.ShowDialog(mainWindow) == DialogResult.OK ? dlg.SelectedObject : null;
			}
		}

		/// <summary>
		/// The Edit Reference Set Details item for a lexical-relation row: absent unless the
		/// row's object is a relation; enabled then.
		/// </summary>
		internal DetailMenuItem EditReferenceItem(DetailMenuLeaf leaf, DetailField field)
		{
			if (!(RowObjectOf(field) is ILexReference))
				return null;
			return new DetailMenuItem(XCoreMenuBridge.StripAccelerator(leaf.Label), isEnabled: true, isChecked: false,
				children: null, execute: () => EditReferenceDetails(field));
		}

		/// <summary>
		/// Gives the row's relation the name and comment the user enters through
		/// <see cref="RelationDetailsEdited"/>: pending edits settle first, the change is one
		/// undoable step, and the view re-shows.
		/// </summary>
		/// <returns>True when the relation changed.</returns>
		internal bool EditReferenceDetails(DetailField field)
		{
			SettleDetailEdits();
			if (!(RowObjectOf(field) is ILexReference relation))
			{
				Logger.WriteEvent("Edit Reference Set Details did nothing: the row's object " + field?.ObjectHvo + " is no relation.");
				return false;
			}
			var details = (RelationDetailsEdited ?? EditRelationDetailsWithDialog)(relation);
			if (details == null)
				return false;
			LexicalRelationEditing.SetDetails(relation, details.Name, details.Comment);
			OnAvaloniaDetailEditCompleted(this, EventArgs.Empty);
			return true;
		}

		private RelationDetails EditRelationDetailsWithDialog(ILexReference relation)
		{
			using (var dlg = new LexReferenceDetailsDlg(m_propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider")))
			{
				dlg.ReferenceName = relation.Name.AnalysisDefaultWritingSystem.Text;
				dlg.ReferenceComment = relation.Comment.AnalysisDefaultWritingSystem.Text;
				return dlg.ShowDialog(m_propertyTable.GetValue<Form>("window")) == DialogResult.OK
					? new RelationDetails { Name = dlg.ReferenceName, Comment = dlg.ReferenceComment }
					: null;
			}
		}

		/// <summary>
		/// The Swap Allomorph with Lexeme Form item for the row: absent unless the row's
		/// object is a form; enabled when it is an allomorph of an entry that has a lexeme
		/// form, disabled otherwise.
		/// </summary>
		internal DetailMenuItem SwapAllomorphItem(DetailMenuLeaf leaf, DetailField field)
		{
			var form = FormOf(field);
			if (form == null)
				return null;
			var label = XCoreMenuBridge.StripAccelerator(leaf.Label);
			if (!CanSwapAllomorph(form))
				return DetailMenuItem.Disabled(label);
			return new DetailMenuItem(label, isEnabled: true, isChecked: false, children: null,
				execute: () => SwapAllomorph(field, leaf.Command));
		}

		// Whether the form is an allomorph of an entry with a lexeme form to swap it with.
		private static bool CanSwapAllomorph(IMoForm form)
		{
			var entry = form.OwnerOfClass<ILexEntry>();
			return LexemeFormConversion.CanSwap(entry) && entry.AlternateFormsOS.Contains(form);
		}

		/// <summary>
		/// Makes the row's allomorph its entry's lexeme form, the old lexeme form taking its
		/// place: pending edits settle first, the swap is one undoable step, and the re-show
		/// focuses the form's new row.
		/// </summary>
		/// <returns>True when the forms were swapped.</returns>
		internal bool SwapAllomorph(DetailField field, Command command)
		{
			SettleDetailEdits();
			var form = FormOf(field);
			if (form == null || !CanSwapAllomorph(form))
				return false;
			LexemeFormConversion.Swap(form.OwnerOfClass<ILexEntry>(), form, command.UndoText, command.RedoText);
			m_avaloniaEntryForm?.FocusObjectOnNextShow(form.Hvo);
			OnAvaloniaDetailEditCompleted(this, EventArgs.Empty);
			return true;
		}
	}
}
