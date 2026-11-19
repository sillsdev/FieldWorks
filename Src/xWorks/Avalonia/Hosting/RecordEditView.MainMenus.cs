// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Linq;
using System.Windows.Forms;
using SIL.FieldWorks.Common.DetailRules;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.Reporting;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// The Avalonia detail view's answers to commands outside its own menus: View > Show Hidden
	/// Fields and Show Dictionary Preview in every tool, in Lexicon Edit the Insert menu's and
	/// pane bar's inserts and conversions, in Category Edit the Insert menu's inserts, and in
	/// Notebook Edit Find in Dictionary, Add to Dictionary and Demote Record. They act on the
	/// current row, the row of the shown record that last had keyboard focus, else on the
	/// shown record itself.
	/// </summary>
	public partial class RecordEditView
	{
		/// <summary>The tool whose inserts and conversions this view answers.</summary>
		internal const string LexiconEditTool = "lexiconEdit";

		/// <summary>The message of the Insert menu's Insert Variant.</summary>
		internal const string InsertVariantOfShownEntryMessage = "InsertItemViaBackrefVector";

		/// <summary>The message of Lexeme Form has components.</summary>
		internal const string ConvertIntoComplexFormMessage = "ConvertEntryIntoComplexForm";

		/// <summary>The message of Lexeme Form is a variant.</summary>
		internal const string ConvertIntoVariantMessage = "ConvertEntryIntoVariant";

		/// <summary>The boolean property of View > Show Dictionary Preview.</summary>
		internal const string ShowDictionaryPreviewProperty = "Show_DictionaryPubPreview";

		// The field of an entry ref whose row a conversion focuses: Components, or Variant of.
		private const string ComponentLexemesField = "ComponentLexemes";

		private IDisposable m_commandAuthorityRegistration;

		/// <summary>
		/// Supplies the current row: the product asks the host for the row that last had
		/// keyboard focus; a headless test names one.
		/// </summary>
		internal Func<DetailField> CurrentRowSource { get; set; }

		/// <summary>
		/// The native answers of this view to the main menu, toolbars and pane bar, live while
		/// this view is in the window that owns its property table.
		/// </summary>
		internal DetailCommandAuthority CreateCommandAuthority()
		{
			var authority = new DetailCommandAuthority(() => !IsDisposed && IsInOwningWindow);
			authority.AnswerProperty(PropertyConstants.ShowHiddenFields,
				() => m_propertyTable.GetBoolProperty("AllowShowNormalFields", true), IsShowingHiddenFields, ToggleShowHiddenFields);
			authority.AnswerProperty(ShowDictionaryPreviewProperty,
				() => DictionaryPreviewAvailability.IsOffered(m_propertyTable.GetStringProperty("areaChoice", null),
					m_propertyTable.GetStringProperty("ToolForAreaNamed_lexicon", null)));
			if (CurrentTool == NotebookEditTool)
			{
				authority.Answer(LexiconLookupMessage, leaf => MainMenuTextWordItem(leaf, addToLexicon: false));
				authority.Answer(AddToLexiconMessage, leaf => MainMenuTextWordItem(leaf, addToLexicon: true));
				authority.Answer(DemoteRecordMessage, DemoteRecordItem);
				return authority;
			}
			if (CurrentTool != LexiconEditTool && CurrentTool != CategoryEditTool)
				return authority;
			authority.Answer(InsertMessage, MainMenuInsertItem);
			if (CurrentTool == CategoryEditTool)
				return authority;
			authority.Answer(InsertMediaFileMessage, leaf => InsertMediaItem(leaf, MainMenuRow()));
			authority.Answer(InsertPictureMessage, leaf => InsertPictureItem(leaf, MainMenuRow()));
			authority.Answer(InsertVariantOfShownEntryMessage, InsertVariantOfShownEntryItem);
			authority.Answer(ConvertIntoComplexFormMessage, leaf => ConvertEntryItem(leaf, complexForm: true));
			authority.Answer(ConvertIntoVariantMessage, leaf => ConvertEntryItem(leaf, complexForm: false));
			return authority;
		}

		// Registers this view's command authority while its Avalonia detail view is active.
		private void RegisterCommandAuthority()
		{
			if (m_commandAuthorityRegistration == null && m_propertyTable != null)
				m_commandAuthorityRegistration = CommandAuthorities.Register(m_propertyTable, CreateCommandAuthority());
		}

		private void UnregisterCommandAuthority()
		{
			m_commandAuthorityRegistration?.Dispose();
			m_commandAuthorityRegistration = null;
		}

		private string CurrentTool => m_propertyTable?.GetStringProperty("currentContentControl", null);

		// A pop-up tool window shares the main window's property table; its view answers nothing.
		private bool IsInOwningWindow
		{
			get
			{
				var window = m_propertyTable?.GetValue<object>("window") as Form;
				return window != null && FindForm() == window;
			}
		}

		/// <summary>
		/// The row a main-menu or pane-bar command acts on: the current row when its object is
		/// the shown record or inside it, else the shown record's first row; null with no shown
		/// record.
		/// </summary>
		internal DetailField MainMenuRow()
		{
			var root = ResolveShownRecord(Clerk?.CurrentObject);
			var shown = m_avaloniaEntryForm?.ShownDetail;
			if (root == null || shown == null)
				return null;
			var current = (CurrentRowSource ?? (() => m_avaloniaEntryForm?.CurrentRow))();
			for (var obj = RowObjectOf(current); obj != null; obj = obj.Owner)
			{
				if (obj == root)
					return current;
			}
			return shown.Fields.FirstOrDefault(f => f.ObjectHvo == root.Hvo);
		}

		/// <summary>
		/// The Insert item of the main menu or pane bar: the row's own Insert item for
		/// <see cref="MainMenuRow"/>, disabled unless the Insert menu's display rule lets the
		/// shown record or the row's object take the new object.
		/// </summary>
		internal DetailMenuItem MainMenuInsertItem(DetailMenuLeaf leaf)
		{
			var row = MainMenuRow();
			if (row == null || !InsertLeafInScope(leaf))
				return null;
			var item = InsertItem(leaf, row);
			if (item == null || !item.IsEnabled)
				return item;
			var root = ResolveShownRecord(Clerk?.CurrentObject);
			return DetailObjectInsertion.CanInsert(root, RowObjectOf(row), leaf.CommandId, leaf.Command.GetParameter("field"),
				out _)
				? item
				: DetailMenuItem.Disabled(item.Label);
		}

		/// <summary>
		/// The Insert menu's Insert Variant: enabled when the command applies to the shown
		/// record, an entry, whatever row is current.
		/// </summary>
		internal DetailMenuItem InsertVariantOfShownEntryItem(DetailMenuLeaf leaf)
		{
			var label = XCoreMenuBridge.StripAccelerator(leaf.Label);
			if (!(ShownRecordTarget(leaf.Command) is ILexEntry)
				|| !DetailObjectInsertion.IsVariantInsert(leaf.Command.GetParameter("fieldName", null),
					leaf.Command.GetParameter("className", null)))
			{
				return DetailMenuItem.Disabled(label);
			}
			return new DetailMenuItem(label, isEnabled: true, isChecked: false, children: null,
				execute: () => InsertVariantOfShownEntry(leaf.Command));
		}

		/// <summary>
		/// Adds a variant to the shown entry through <see cref="VariantToInsert"/>: pending
		/// edits settle first, and the re-show focuses the variant's Variant Form editor.
		/// </summary>
		/// <returns>The variant's entry ref, or null when nothing was added.</returns>
		internal ILexEntryRef InsertVariantOfShownEntry(Command command)
		{
			SettleDetailEdits();
			if (!(ShownRecordTarget(command) is ILexEntry entry))
			{
				Logger.WriteEvent("Insert Variant did nothing: the command does not apply to the shown record.");
				return null;
			}
			return AddVariant(entry);
		}

		/// <summary>
		/// The Lexeme Form has components or Lexeme Form is a variant item: enabled when the
		/// command applies to the shown record, an entry, and, for a complex form, the entry
		/// is not one yet.
		/// </summary>
		/// <param name="complexForm">True for Lexeme Form has components.</param>
		internal DetailMenuItem ConvertEntryItem(DetailMenuLeaf leaf, bool complexForm)
		{
			var label = XCoreMenuBridge.StripAccelerator(leaf.Label);
			if (ConvertibleEntry(leaf.Command, complexForm) == null)
				return DetailMenuItem.Disabled(label);
			return new DetailMenuItem(label, isEnabled: true, isChecked: false, children: null,
				execute: () => ConvertEntry(leaf.Command, complexForm));
		}

		/// <summary>
		/// Makes the shown entry a complex form or a variant: pending edits settle first, the
		/// new entry ref is one undoable step, and the re-show focuses its Components or
		/// Variant of row.
		/// </summary>
		/// <param name="complexForm">True for a complex form.</param>
		/// <returns>The new entry ref, or null when the command does not apply.</returns>
		internal ILexEntryRef ConvertEntry(Command command, bool complexForm)
		{
			SettleDetailEdits();
			var entry = ConvertibleEntry(command, complexForm);
			if (entry == null)
			{
				Logger.WriteEvent(command.Id + " did nothing: the shown record cannot convert.");
				return null;
			}
			var entryRef = complexForm
				? EntryConversion.ConvertIntoComplexForm(entry, command.UndoText, command.RedoText)
				: EntryConversion.ConvertIntoVariant(entry, command.UndoText, command.RedoText);
			m_avaloniaEntryForm?.FocusObjectOnNextShow(entryRef.Hvo, ComponentLexemesField);
			OnAvaloniaDetailEditCompleted(this, EventArgs.Empty);
			return entryRef;
		}

		// The shown entry when the conversion applies to it; null otherwise.
		private ILexEntry ConvertibleEntry(Command command, bool complexForm)
		{
			var entry = ShownRecordTarget(command) as ILexEntry;
			return entry == null || complexForm && !EntryConversion.CanConvertIntoComplexForm(entry) ? null : entry;
		}

		// The shown record when a command that acts on it applies to it in this tool; null
		// otherwise.
		private ICmObject ShownRecordTarget(Command command)
		{
			var root = ResolveShownRecord(Clerk?.CurrentObject);
			return ShownRecordCommandScope.Applies(root, command.GetParameter("className", null),
				command.GetParameter("restrictToTool", null), CurrentTool)
				? root
				: null;
		}

		// Whether this tool shows hidden fields, the check mark of View > Show Hidden Fields.
		private bool IsShowingHiddenFields()
			=> m_propertyTable.GetBoolProperty(PropertyConstants.ShowHiddenFieldsSpecificPrefix + CurrentTool, false,
				PropertyTable.SettingsGroup.LocalSettings);

		// Flips this tool's own Show Hidden Fields setting, as its pane-bar item does.
		private void ToggleShowHiddenFields()
			=> m_propertyTable.SetProperty(PropertyConstants.ShowHiddenFieldsSpecificPrefix + CurrentTool,
				!IsShowingHiddenFields(), PropertyTable.SettingsGroup.LocalSettings, true);
	}
}
