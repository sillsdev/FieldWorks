// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.FieldWorks.FdoUi;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;
using SIL.Reporting;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// The reference-vector row menus of the Avalonia detail view: the per-item menu (the
	/// reference-choices menu of the clicked item, answered by that item's object UI as a
	/// temporary colleague) and the Move Left / Move Right commands, which act on the row's
	/// current item through the detail edit context.
	/// </summary>
	public partial class RecordEditView
	{
		private const string MoveLeftCommandId = "CmdMoveTargetToPreviousInSequence";
		private const string MoveRightCommandId = "CmdMoveTargetToNextInSequence";

		/// <summary>
		/// Shows the per-item menu of a reference-vector row, or for a Ctrl+click runs its
		/// default jump command. Never throws: failures are logged.
		/// </summary>
		internal void OnDetailItemMenuRequested(DetailMenuRequest request)
		{
			if (IsDisposed || m_mediator == null || m_avaloniaEntryForm == null)
				return;
			try
			{
				if (request.IsDefaultActivation)
				{
					var ui = ResolveItemUi(request);
					if (ui == null)
						return;
					// The item's object UI runs the first enabled jump itself, then disposes.
					SyncMenuCommandAdapter(request.Field);
					ui.HandleCtrlClick(this);
					return;
				}

				var items = BuildReferenceItemMenu(request, out var colleague);
				if (colleague == null)
					return;
				if (items.Count == 0)
				{
					colleague.Dispose();
					return;
				}
				// The colleague answers the clicked command, which runs as the menu closes, so
				// its disposal is queued behind the close.
				try
				{
					m_avaloniaEntryForm.ShowContextMenu(items, request.AnchorControl, request.OpenAtPointer,
						() => Avalonia.Threading.Dispatcher.UIThread.Post(colleague.Dispose,
							Avalonia.Threading.DispatcherPriority.Background));
				}
				catch
				{
					colleague.Dispose();
					throw;
				}
			}
			catch (Exception e)
			{
				Logger.WriteError("Detail item menu failed.", e);
			}
		}

		/// <summary>
		/// Materializes the item menu for the request's selected item: the object UI of that
		/// item joins the mediator as a temporary colleague (it answers the Show-in-tool jumps),
		/// the first enabled jump is labeled as the Ctrl+click default, and the Move commands are
		/// retargeted to the row's current item. Empty, with a null colleague, when the item
		/// cannot be resolved.
		/// </summary>
		/// <param name="request">The item-menu request; its selected item is the target.</param>
		/// <param name="colleague">The item's object UI, registered on the mediator; the caller
		/// disposes it once the menu has closed.</param>
		internal IReadOnlyList<DetailMenuItem> BuildReferenceItemMenu(DetailMenuRequest request,
			out CmObjectUi colleague)
		{
			colleague = null;
			var ui = ResolveItemUi(request);
			if (ui == null)
				return Array.Empty<DetailMenuItem>();

			SyncMenuCommandAdapter(request.Field);

			var registry = new OverrideCommandRegistry();
			var marked = false;
			// The first enabled jump is the Ctrl+click default; its label says so.
			registry.Add(
				choice => choice is CommandChoice command && string.Equals(command.Message,
					CmObjectUi.JumpToToolMessage, StringComparison.Ordinal),
				(choice, display) =>
				{
					if (marked || !display.Enabled)
						return null;
					marked = true;
					return new DetailMenuItem(
						XCoreMenuBridge.StripAccelerator(display.Text) + CmObjectUi.CtrlClickSuffix,
						isEnabled: true, isChecked: display.Checked, children: null,
						execute: () => choice.OnClick(null, EventArgs.Empty));
				});
			AddMoveCommands(registry, request);

			var window = m_propertyTable.GetValue<XWindow>("window");
			IReadOnlyList<DetailMenuItem> items;
			try
			{
				items = XCoreMenuBridge.CreateMenuItems(window, new[] { ui.ContextMenuId },
					registry.TryBuild, ui);
			}
			catch
			{
				ui.Dispose(); // a failed menu must not leave the colleague on the mediator
				throw;
			}
			colleague = ui;
			return items;
		}

		// Points the hidden command adapter at the row, so the row's own slice answers the
		// commands that need slice context; when that fails, those commands stay hidden.
		private void SyncMenuCommandAdapter(DetailField field)
		{
			try
			{
				EnsureMenuCommandAdapter(field.ObjectHvo, field.Field);
			}
			catch (Exception adapterError)
			{
				Logger.WriteError("Detail item menu command adapter failed; the commands that need "
					+ "the hidden colleague chain stay hidden.", adapterError);
			}
		}

		/// <summary>
		/// The object UI of the request's selected item, wired to this view's mediator and
		/// property table; null when the row's object, field, or item cannot be resolved.
		/// </summary>
		internal CmObjectUi ResolveItemUi(DetailMenuRequest request)
		{
			var field = request?.Field;
			if (field == null || string.IsNullOrEmpty(request.SelectedItemKey)
				|| !Cache.ServiceLocator.ObjectRepository.TryGetObject(field.ObjectHvo, out var rootObj))
			{
				return null;
			}
			var mdc = (IFwMetaDataCacheManaged)Cache.MetaDataCacheAccessor;
			var flid = mdc.GetFieldId2(rootObj.ClassID, field.Field, true);
			var targetHvo = ResolveVectorItem(rootObj, flid, request.SelectedItemKey);
			if (targetHvo == 0)
				return null;
			var ui = ReferenceBaseUi.MakeUi(Cache, rootObj, flid, targetHvo);
			if (ui == null)
				return null;
			ui.Mediator = m_mediator;
			ui.PropTable = m_propertyTable;
			return ui;
		}

		// The vector item a row's option key names: the item itself, or for a back-reference
		// row (whose items are entry refs shown by their owning entry) the ref that entry owns.
		private int ResolveVectorItem(ICmObject rootObj, int flid, string key)
		{
			var sda = Cache.DomainDataByFlid;
			var repository = Cache.ServiceLocator.ObjectRepository;
			var size = sda.get_VecSize(rootObj.Hvo, flid);
			for (var i = 0; i < size; i++)
			{
				var hvo = sda.get_VecItem(rootObj.Hvo, flid, i);
				if (!repository.TryGetObject(hvo, out var item))
					continue;
				if (string.Equals(item.Guid.ToString(), key, StringComparison.Ordinal))
					return hvo;
				if (item is ILexEntryRef && string.Equals(
					item.OwnerOfClass<ILexEntry>()?.Guid.ToString(), key, StringComparison.Ordinal))
				{
					return hvo;
				}
			}
			return 0;
		}

		/// <summary>
		/// Registers Move Left / Move Right retargeted to the request's current item; registers
		/// nothing for a row that is not a reference vector.
		/// </summary>
		internal void AddMoveCommands(OverrideCommandRegistry registry, DetailMenuRequest request)
		{
			if (request?.Field == null || request.Field.Kind != DetailFieldKind.ReferenceVector)
				return;
			registry.Add(MoveLeftCommandId, (choice, display) => MoveCommandItem(request, display, forward: false));
			registry.Add(MoveRightCommandId, (choice, display) => MoveCommandItem(request, display, forward: true));
		}

		// A Move Left / Move Right item for the request's current item, enabled only when the
		// row can be reordered and the item is not at that end.
		private DetailMenuItem MoveCommandItem(DetailMenuRequest request, UIItemDisplayProperties display,
			bool forward)
		{
			var field = request.Field;
			var index = request.SelectedItemIndex;
			var canMove = field.Kind == DetailFieldKind.ReferenceVector && field.IsEditable
				&& field.CanReorderItems
				&& index >= 0 && (forward ? index < field.Items.Count - 1 : index > 0);
			var key = request.SelectedItemKey;
			return new DetailMenuItem(XCoreMenuBridge.StripAccelerator(display.Text), isEnabled: canMove,
				isChecked: false, children: null,
				execute: canMove ? (Action)(() => MoveReferenceItem(field, key, forward)) : null);
		}

		/// <summary>
		/// Moves a reference-vector item one place through the detail edit context and completes
		/// the gesture the way every other detail edit does: validate-then-commit, then one
		/// coalesced re-show, through which the moved item stays current.
		/// </summary>
		internal void MoveReferenceItem(DetailField field, string key, bool forward)
		{
			// Pending edits settle first, so the move is its own undo step and an invalid
			// pending edit rolls back with its own warning instead of taking the move with it.
			m_detailEditContext.Settle();
			var context = m_detailEditContext.Current;
			if (context == null || !context.TryMoveReferenceItem(field, key, forward))
				return;
			// A move that fails validation rolls back and never re-shows, so the focus
			// request is made only once the commit is known to have succeeded; nothing
			// clears a request the re-show does not consume.
			if (m_detailEditContext.Settle().Count != 0)
				return;
			// The menu took keyboard focus; the re-show hands it to the moved item.
			m_avaloniaEntryForm?.FocusVectorItemOnNextShow();
			OnAvaloniaDetailEditCompleted(this, EventArgs.Empty);
		}
	}
}
