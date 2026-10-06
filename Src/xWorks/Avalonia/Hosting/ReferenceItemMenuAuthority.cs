// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using SIL.FieldWorks.Common.DetailRules;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.FieldWorks.FdoUi;
using SIL.LCModel;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// What the host does for the per-item menu of a reference-vector row: the commands that
	/// change the model or navigate, and the tool the row is shown in.
	/// </summary>
	internal interface IReferenceItemMenuHost
	{
		/// <summary>The current tool, e.g. "lexiconEdit"; null when none is current.</summary>
		string CurrentTool { get; }

		/// <summary>Moves the item with the given key one place within the row.</summary>
		void MoveItem(DetailField field, string key, bool forward);

		/// <summary>
		/// Runs a jump that leaves the record, after the record's pending edits settle.
		/// </summary>
		void RunJump(Action jump);

		/// <summary>
		/// Opens a tool filtered to the records carrying an anthropology category.
		/// </summary>
		void FilterByAnthroItem(string tool, int anthroItemHvo);

		/// <summary>
		/// Marks or unmarks the component as showing the complex form's subentry.
		/// </summary>
		void ToggleSubentryUnderComponent(ILexEntryRef complexFormRef, ICmObject component);

		/// <summary>
		/// Marks or unmarks the component as showing the referenced complex form.
		/// </summary>
		void ToggleShowComplexFormIn(ILexEntryRef complexFormRef, ICmObject component);
	}

	/// <summary>
	/// The native authority for the per-item menu of a reference-vector row: the Show-in-tool
	/// jumps of the clicked item, the two anthropology-category filter jumps, the two
	/// dictionary-visibility marks of a complex form, and Move Left / Move Right. Every answer
	/// comes from the row, the clicked item and the item's object UI, which is called directly
	/// rather than registered on the mediator, so the menu needs neither the hidden DataTree
	/// adapter nor a colleague.
	/// </summary>
	internal sealed class ReferenceItemMenuAuthority : IDetailMenuAuthority
	{
		internal const string MenuId = "mnuReferenceChoices";
		internal const string ShowSubentryUnderComponentCommandId = "CmdShowSubentryUnderComponent";
		internal const string VisibleComplexFormCommandId = "CmdVisibleComplexForm";
		internal const string FilterLexiconCommandId = "CmdJumpToLexiconEditWithFilter";
		internal const string FilterNotebookCommandId = "CmdJumpToNotebookEditWithFilter";
		internal const string ComponentLexemesField = "ComponentLexemes";
		internal const string ComplexFormEntriesField = "ComplexFormEntries";

		private readonly DetailMenuRequest _request;
		private readonly CmObjectUi _itemUi;
		private readonly ICmObject _rowObject;
		private readonly ICmObject _item;
		private readonly IReferenceItemMenuHost _host;
		// The Ctrl+click default's command id, so every build of the menu marks the same jump.
		private string _defaultCommandId;

		/// <summary>Creates the authority for one item-menu request.</summary>
		/// <param name="request">The menu request; its field is the row.</param>
		/// <param name="itemUi">The object UI of the clicked item within the row's reference,
		/// which answers the jumps.</param>
		/// <param name="rowObject">The object the row's reference belongs to.</param>
		/// <param name="item">The clicked item.</param>
		/// <param name="host">The host the commands act through.</param>
		public ReferenceItemMenuAuthority(DetailMenuRequest request, CmObjectUi itemUi, ICmObject rowObject,
			ICmObject item, IReferenceItemMenuHost host)
		{
			_request = request ?? throw new ArgumentNullException(nameof(request));
			_itemUi = itemUi ?? throw new ArgumentNullException(nameof(itemUi));
			_rowObject = rowObject ?? throw new ArgumentNullException(nameof(rowObject));
			_item = item ?? throw new ArgumentNullException(nameof(item));
			_host = host ?? throw new ArgumentNullException(nameof(host));
		}

		/// <summary>
		/// The action of the first enabled jump in menu order, which a Ctrl+click runs; null
		/// until the menu has been built, or when no jump is enabled.
		/// </summary>
		public Action DefaultActivation { get; private set; }

		public bool Owns(string menuId) => string.Equals(menuId, MenuId, StringComparison.Ordinal);

		public DetailMenuItem Build(string menuId, ChoiceBase leaf)
		{
			if (leaf == null)
				throw new ArgumentNullException(nameof(leaf));
			var label = XCoreMenuBridge.StripAccelerator(leaf.Label);
			var command = leaf as CommandChoice;
			switch (leaf.HelpId)
			{
				case ReorderVectorMenuAuthority.MoveLeftCommandId:
					return MoveItem(label, forward: false);
				case ReorderVectorMenuAuthority.MoveRightCommandId:
					return MoveItem(label, forward: true);
				case ShowSubentryUnderComponentCommandId:
					return ShowSubentryItem(label, ToolOf(command));
				case VisibleComplexFormCommandId:
					return VisibleComplexFormItem(label);
				case FilterLexiconCommandId:
				case FilterNotebookCommandId:
					return FilterItem(label, ToolOf(command));
				default:
					if (command != null && string.Equals(command.Message, CmObjectUi.JumpToToolMessage,
						StringComparison.Ordinal))
					{
						return JumpItem(command);
					}
					throw new InvalidOperationException(string.Format(
						"Menu '{0}' has a leaf '{1}' this authority does not answer.", menuId, leaf.HelpId));
			}
		}

		// The owned id carries no list-populated submenu.
		public IReadOnlyList<DetailMenuItem> BuildList(string menuId, string listId)
			=> throw new InvalidOperationException(string.Format(
				"Menu '{0}' has a list submenu '{1}' this authority does not answer.",
				menuId, listId));

		// The tool a command's parameters name, or null.
		private static string ToolOf(CommandChoice command)
			=> command == null ? null : XmlUtils.GetOptionalAttributeValue(command.CommandObject.Parameters[0], "tool");

		// The item's object UI decides a jump as it would for any menu; the first enabled jump
		// is the Ctrl+click default and its label says so.
		private DetailMenuItem JumpItem(CommandChoice choice)
		{
			var command = choice.CommandObject;
			var display = new UIItemDisplayProperties(null, choice.Label, true, command.IconName, true);
			_itemUi.OnDisplayJumpToTool(command, ref display);
			// The display sets the shared command's target; the object UI resolves its own on
			// execute, so clear it here rather than leave it for a later menu to pick up.
			command.TargetId = Guid.Empty;
			if (!display.Visible)
				return null;
			var label = XCoreMenuBridge.StripAccelerator(display.Text);
			if (!display.Enabled)
				return new DetailMenuItem(label, isEnabled: false, display.Checked);
			Action execute = () => _host.RunJump(() => _itemUi.OnJumpToTool(command));
			if (_defaultCommandId == null || string.Equals(_defaultCommandId, choice.HelpId, StringComparison.Ordinal))
			{
				_defaultCommandId = choice.HelpId;
				DefaultActivation = execute;
				label += CmObjectUi.CtrlClickSuffix;
			}
			return new DetailMenuItem(label, isEnabled: true, display.Checked, children: null, execute);
		}

		// Offered whenever the row reorders, enabled only for a movable current item.
		private DetailMenuItem MoveItem(string label, bool forward)
			=> ReorderVectorMenuAuthority.CanReorderRow(_request.Field)
				? ReorderVectorMenuAuthority.BuildMoveItem(_request, label, forward, _host.MoveItem)
				: null;

		// Offered on a complex form's Components row, in the tool the command names, for a
		// component that is an entry or a sense.
		private DetailMenuItem ShowSubentryItem(string label, string tool)
		{
			if (!(_rowObject is ILexEntryRef complexFormRef)
				|| !string.Equals(_request.Field.Field, ComponentLexemesField, StringComparison.Ordinal)
				|| (tool != null && !string.Equals(tool, _host.CurrentTool, StringComparison.Ordinal))
				|| !ComplexFormVisibility.CanShowSubentryUnderComponent(complexFormRef, _item))
			{
				return null;
			}
			var component = _item;
			return new DetailMenuItem(label, isEnabled: true,
				ComplexFormVisibility.ShowsSubentryUnderComponent(complexFormRef, component), children: null,
				() => _host.ToggleSubentryUnderComponent(complexFormRef, component));
		}

		// Offered on an entry's or sense's Complex Forms row for an item that is a complex form.
		private DetailMenuItem VisibleComplexFormItem(string label)
		{
			if (!(_rowObject is ILexEntry || _rowObject is ILexSense)
				|| !string.Equals(_request.Field.Field, ComplexFormEntriesField, StringComparison.Ordinal))
			{
				return null;
			}
			var complexFormRef = ComplexFormVisibility.ComplexFormRefOf(_item as ILexEntry);
			if (complexFormRef == null)
				return null;
			var component = _rowObject;
			return new DetailMenuItem(label, isEnabled: true,
				ComplexFormVisibility.ShowsComplexFormIn(complexFormRef, component), children: null,
				() => _host.ToggleShowComplexFormIn(complexFormRef, component));
		}

		// Offered on the Anthropology Categories row only; always enabled there.
		private DetailMenuItem FilterItem(string label, string tool)
		{
			if (tool == null || !AnthroItemFilterLink.Applies(_request.Field.Field))
				return null;
			var hvo = _item.Hvo;
			return new DetailMenuItem(label, isEnabled: true, isChecked: false, children: null,
				() => _host.FilterByAnthroItem(tool, hvo));
		}
	}
}
