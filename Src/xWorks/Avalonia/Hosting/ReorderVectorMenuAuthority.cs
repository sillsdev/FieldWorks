// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// The native authority for the reorder-vector menu (Move Left, Move Right, Alphabetical
	/// Order) of a row whose items have an order. Every answer comes from the row's
	/// <see cref="DetailField"/> and the request's current item, so the menu needs neither the
	/// hidden DataTree adapter nor a WinForms selection.
	/// </summary>
	internal sealed class ReorderVectorMenuAuthority : IDetailMenuAuthority
	{
		internal const string MenuId = "mnuReorderVector";
		internal const string MoveLeftCommandId = "CmdMoveTargetToPreviousInSequence";
		internal const string MoveRightCommandId = "CmdMoveTargetToNextInSequence";
		internal const string AlphabeticalOrderCommandId = "CmdAlphabeticalOrder";

		private readonly DetailMenuRequest _request;
		private readonly Action<DetailField, string, bool> _moveItem;
		private readonly Action<DetailField> _resetOrder;

		/// <summary>Creates the authority for one menu request.</summary>
		/// <param name="request">The menu request; its field is the row, its selected item the
		/// move target.</param>
		/// <param name="moveItem">Moves (field, item key, forward) one place.</param>
		/// <param name="resetOrder">Discards the field's stored item order.</param>
		public ReorderVectorMenuAuthority(DetailMenuRequest request,
			Action<DetailField, string, bool> moveItem, Action<DetailField> resetOrder)
		{
			_request = request ?? throw new ArgumentNullException(nameof(request));
			_moveItem = moveItem ?? throw new ArgumentNullException(nameof(moveItem));
			_resetOrder = resetOrder ?? throw new ArgumentNullException(nameof(resetOrder));
		}

		public bool Owns(string menuId) => string.Equals(menuId, MenuId, StringComparison.Ordinal);

		public DetailMenuItem Build(string menuId, ChoiceBase leaf)
		{
			if (leaf == null)
				throw new ArgumentNullException(nameof(leaf));
			var label = XCoreMenuBridge.StripAccelerator(leaf.Label);
			switch (leaf.HelpId)
			{
				case MoveLeftCommandId:
					return MoveItem(label, forward: false);
				case MoveRightCommandId:
					return MoveItem(label, forward: true);
				case AlphabeticalOrderCommandId:
					return AlphabeticalOrderItem(label);
				default:
					throw new InvalidOperationException(string.Format(
						"Menu '{0}' has a leaf '{1}' this authority does not answer.", menuId, leaf.HelpId));
			}
		}

		/// <summary>
		/// Whether the row's items may be reordered: an editable reference-vector row whose
		/// property is a real reference sequence or is marked reorderable. Move Left and Move
		/// Right are absent from any other row, including a read-only row that binds the
		/// menu (WinForms hides them there by label).
		/// </summary>
		internal static bool CanReorderRow(DetailField field)
			=> field != null && field.Kind == DetailFieldKind.ReferenceVector && field.CanReorderItems;

		/// <summary>
		/// Whether the item at <paramref name="index"/> can move one place: the row reorders,
		/// is editable, and the item is current and not already at that end.
		/// </summary>
		internal static bool CanMoveItem(DetailField field, int index, bool forward)
			=> CanReorderRow(field) && field.IsEditable && index >= 0
				&& (forward ? index < field.Items.Count - 1 : index > 0);

		/// <summary>
		/// A Move Left or Move Right item for the request's current item, enabled only when
		/// that item can move one place; <paramref name="move"/> receives (field, item key,
		/// forward).
		/// </summary>
		internal static DetailMenuItem BuildMoveItem(DetailMenuRequest request, string label, bool forward,
			Action<DetailField, string, bool> move)
		{
			var field = request.Field;
			var canMove = CanMoveItem(field, request.SelectedItemIndex, forward);
			var key = request.SelectedItemKey;
			return new DetailMenuItem(label, isEnabled: canMove, isChecked: false, children: null,
				execute: canMove ? (Action)(() => move(field, key, forward)) : null);
		}

		// Offered whenever the row reorders, enabled only for a movable current item.
		private DetailMenuItem MoveItem(string label, bool forward)
			=> CanReorderRow(_request.Field) ? BuildMoveItem(_request, label, forward, _moveItem) : null;

		// Offered, and enabled, exactly when the row's order is a resettable virtual ordering.
		private DetailMenuItem AlphabeticalOrderItem(string label)
		{
			var field = _request.Field;
			if (field == null || !field.CanResetItemOrder)
				return null;
			return new DetailMenuItem(label, isEnabled: true, isChecked: false, children: null,
				execute: () => _resetOrder(field));
		}
	}
}
