// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using SIL.FieldWorks.Common.DetailRules;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// The native authority for a multi-writing-system row's label menu: the Writing Systems
	/// submenu answered from the row and the shared writing-system rule, and the Field
	/// Visibility, Move Field and Help leaves delegated to the per-object authority. Owns its
	/// id only on a genuine multi-string row; any other row that binds it keeps the mediator.
	/// </summary>
	internal sealed class MultiStringMenuAuthority : IDetailMenuAuthority
	{
		internal const string MenuId = RecordEditView.MultiStringSliceMenuId;
		internal const string WritingSystemListId = "WritingSystemOptionsForSlice";
		internal const string ShowAllCommandId = "CmdDataTree-WritingSystemMenu-ShowAllRightNow";
		internal const string ConfigureCommandId = "CmdDataTree-WritingSystemMenu-Configure";

		private readonly DetailField _field;
		private readonly IDetailMenuAuthority _sharedLeaves;
		private readonly Func<DetailField, IReadOnlyList<WritingSystemMenuOption>> _menu;
		private readonly Action<DetailField, IReadOnlyList<string>> _show;
		private readonly Func<string, DetailField, DetailMenuItem> _showAll;
		private readonly Func<string, DetailField, DetailMenuItem> _configure;

		/// <summary>Creates the authority for one row's menu.</summary>
		/// <param name="field">The row.</param>
		/// <param name="sharedLeaves">Answers the Field Visibility, Move Field and Help leaves
		/// this menu re-declares.</param>
		/// <param name="menu">The row's writing-system options, checked and enabled.</param>
		/// <param name="show">Makes the row show exactly the given writing systems.</param>
		/// <param name="showAll">Builds the Show all right now item from (label, row).</param>
		/// <param name="configure">Builds the Configure item from (label, row).</param>
		public MultiStringMenuAuthority(DetailField field, IDetailMenuAuthority sharedLeaves,
			Func<DetailField, IReadOnlyList<WritingSystemMenuOption>> menu,
			Action<DetailField, IReadOnlyList<string>> show,
			Func<string, DetailField, DetailMenuItem> showAll,
			Func<string, DetailField, DetailMenuItem> configure)
		{
			_field = field ?? throw new ArgumentNullException(nameof(field));
			_sharedLeaves = sharedLeaves ?? throw new ArgumentNullException(nameof(sharedLeaves));
			_menu = menu ?? throw new ArgumentNullException(nameof(menu));
			_show = show ?? throw new ArgumentNullException(nameof(show));
			_showAll = showAll ?? throw new ArgumentNullException(nameof(showAll));
			_configure = configure ?? throw new ArgumentNullException(nameof(configure));
		}

		public bool Owns(string menuId)
			=> _field.IsMultiStringRow && string.Equals(menuId, MenuId, StringComparison.Ordinal);

		public DetailMenuItem Build(string menuId, ChoiceBase leaf)
		{
			if (leaf == null)
				throw new ArgumentNullException(nameof(leaf));
			var label = XCoreMenuBridge.StripAccelerator(leaf.Label);
			switch (leaf.HelpId)
			{
				case ShowAllCommandId:
					return _showAll(label, _field);
				case ConfigureCommandId:
					return _configure(label, _field);
				default:
					// Everything else is the per-object menu's leaf set, re-declared here.
					return _sharedLeaves.Build(menuId, leaf);
			}
		}

		public IReadOnlyList<DetailMenuItem> BuildList(string menuId, string listId)
		{
			if (!string.Equals(listId, WritingSystemListId, StringComparison.Ordinal))
			{
				throw new InvalidOperationException(string.Format(
					"Menu '{0}' has a list submenu '{1}' this authority does not answer.", menuId, listId));
			}
			var options = _menu(_field);
			return options.Select(option => ToggleItem(option, options)).ToList();
		}

		// A toggle flips its own writing system in or out of the shown set and leaves the rest
		// alone. The last shown one is offered disabled, so the set can never be emptied.
		private DetailMenuItem ToggleItem(WritingSystemMenuOption option,
			IReadOnlyList<WritingSystemMenuOption> all)
		{
			return new DetailMenuItem(option.Label, option.CanUncheck, option.IsChecked, children: null,
				execute: option.CanUncheck ? (Action)(() => _show(_field, Toggled(option, all))) : null);
		}

		// The shown set with one writing system flipped, computed only when its item is clicked.
		private static IReadOnlyList<string> Toggled(WritingSystemMenuOption option,
			IReadOnlyList<WritingSystemMenuOption> all)
			=> all
				.Where(o => string.Equals(o.Id, option.Id, StringComparison.OrdinalIgnoreCase)
					? !o.IsChecked
					: o.IsChecked)
				.Select(o => o.Id)
				.ToList();
	}
}
