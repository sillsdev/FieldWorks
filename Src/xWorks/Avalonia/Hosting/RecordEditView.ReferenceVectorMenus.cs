// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using SIL.FieldWorks.Common.DetailRules;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.FieldWorks.FdoUi;
using SIL.LCModel;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Infrastructure;
using SIL.Reporting;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// The reference-vector row menus of the Avalonia detail view: the per-item menu of the
	/// clicked item, answered natively by <see cref="ReferenceItemMenuAuthority"/> when it owns
	/// the item's menu id and otherwise by the item's object UI as a temporary colleague, and
	/// the Move Left / Move Right commands, which act on the row's current item through the
	/// detail edit context.
	/// </summary>
	public partial class RecordEditView : IReferenceItemMenuHost
	{
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
					RunDefaultItemActivation(request);
					return;
				}

				var items = BuildReferenceItemMenu(request, out var itemUi);
				if (itemUi == null)
					return;
				if (items.Count == 0)
				{
					itemUi.Dispose();
					return;
				}
				// The item's object UI answers the clicked command, which runs as the menu
				// closes, so its disposal is queued behind the close.
				try
				{
					m_avaloniaEntryForm.ShowContextMenu(items, request.AnchorControl, request.OpenAtPointer,
						() => Avalonia.Threading.Dispatcher.UIThread.Post(itemUi.Dispose,
							Avalonia.Threading.DispatcherPriority.Background));
				}
				catch
				{
					itemUi.Dispose();
					throw;
				}
			}
			catch (Exception e)
			{
				Logger.WriteError("Detail item menu failed.", e);
			}
		}

		// A Ctrl+click runs the item menu's first enabled jump: taken from the authority for an
		// owned menu, else from the object UI's own Ctrl+click, which needs the adapter.
		private void RunDefaultItemActivation(DetailMenuRequest request)
		{
			var authority = CreateReferenceItemAuthority(request, out var itemUi);
			if (itemUi == null)
				return;
			using (itemUi)
			{
				if (authority.Owns(itemUi.ContextMenuId))
				{
					var window = m_propertyTable.GetValue<XWindow>("window");
					XCoreMenuBridge.CreateMenuItems(window, new[] { itemUi.ContextMenuId }, null, null, authority);
					authority.DefaultActivation?.Invoke();
					return;
				}
				SyncMenuCommandAdapter(request.Field);
				itemUi.HandleCtrlClick(this);
			}
		}

		/// <summary>
		/// Materializes the item menu for the request's selected item. A menu id the native
		/// authority owns is answered from the row and the item alone, with nothing on the
		/// mediator taking part; any other id is built through the item's object UI as a
		/// temporary colleague. Empty, with a null <paramref name="itemUi"/>, when the item
		/// cannot be resolved.
		/// </summary>
		/// <param name="request">The item-menu request; its selected item is the target.</param>
		/// <param name="itemUi">The item's object UI, which the menu's commands run through; the
		/// caller disposes it once the menu has closed.</param>
		internal IReadOnlyList<DetailMenuItem> BuildReferenceItemMenu(DetailMenuRequest request,
			out CmObjectUi itemUi)
		{
			itemUi = null;
			var authority = CreateReferenceItemAuthority(request, out var ui);
			if (ui == null)
				return Array.Empty<DetailMenuItem>();

			IReadOnlyList<DetailMenuItem> items;
			try
			{
				if (authority.Owns(ui.ContextMenuId))
				{
					var window = m_propertyTable.GetValue<XWindow>("window");
					items = XCoreMenuBridge.CreateMenuItems(window, new[] { ui.ContextMenuId }, null, null, authority);
				}
				else
				{
					items = BuildItemMenuThroughTheColleague(request, ui);
				}
			}
			catch
			{
				ui.Dispose(); // a failed menu must not leave the object UI on the mediator
				throw;
			}
			itemUi = ui;
			return items;
		}

		/// <summary>
		/// The item menu built through the mediator: the hidden command adapter is pointed at
		/// the row, the item's object UI joins the mediator as a temporary colleague to answer
		/// the Show-in-tool jumps, the first enabled jump is labeled as the Ctrl+click default,
		/// and the Move commands are retargeted to the row's current item.
		/// </summary>
		/// <param name="request">The item-menu request.</param>
		/// <param name="ui">The item's object UI; registered on the mediator by this call, and
		/// disposed by the caller.</param>
		internal IReadOnlyList<DetailMenuItem> BuildItemMenuThroughTheColleague(DetailMenuRequest request,
			CmObjectUi ui)
		{
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
			return XCoreMenuBridge.CreateMenuItems(window, new[] { ui.ContextMenuId }, registry.TryBuild, ui);
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
		/// The native authority for the item menu of the request's selected item, with the
		/// item's object UI it answers the jumps through; both null when the row's object,
		/// field, or item cannot be resolved.
		/// </summary>
		/// <param name="request">The item-menu request.</param>
		/// <param name="itemUi">The item's object UI, wired to this view's mediator and
		/// property table; the caller disposes it.</param>
		internal ReferenceItemMenuAuthority CreateReferenceItemAuthority(DetailMenuRequest request,
			out CmObjectUi itemUi)
		{
			itemUi = ResolveItemUi(request, out var rowObject, out var item);
			return itemUi == null ? null : new ReferenceItemMenuAuthority(request, itemUi, rowObject, item, this);
		}

		/// <summary>
		/// The object UI of the request's selected item, wired to this view's mediator and
		/// property table; null when the row's object, field, or item cannot be resolved.
		/// </summary>
		internal CmObjectUi ResolveItemUi(DetailMenuRequest request)
			=> ResolveItemUi(request, out _, out _);

		// As ResolveItemUi, also giving the row's object and the resolved item.
		private CmObjectUi ResolveItemUi(DetailMenuRequest request, out ICmObject rowObject, out ICmObject item)
		{
			rowObject = null;
			item = null;
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
			// MakeUi gives a collection the base UI, which names the generic menu, and that
			// drops every command for an environment. Only the collection UI names the
			// environments menu.
			var mdcType = (CellarPropertyType)mdc.GetFieldType(flid);
			var ui = mdcType == CellarPropertyType.ReferenceCollection
				? new ReferenceCollectionUi(Cache, rootObj, flid, targetHvo)
				: ReferenceBaseUi.MakeUi(Cache, rootObj, flid, targetHvo);
			if (ui == null)
				return null;
			ui.Mediator = m_mediator;
			ui.PropTable = m_propertyTable;
			rowObject = rootObj;
			item = Cache.ServiceLocator.ObjectRepository.GetObject(targetHvo);
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
			registry.Add(ReorderVectorMenuAuthority.MoveLeftCommandId,
				(choice, display) => MoveCommandItem(request, display, forward: false));
			registry.Add(ReorderVectorMenuAuthority.MoveRightCommandId,
				(choice, display) => MoveCommandItem(request, display, forward: true));
		}

		// The item menu's Move Left / Move Right, built by the same rule as the label menu's.
		private DetailMenuItem MoveCommandItem(DetailMenuRequest request, UIItemDisplayProperties display,
			bool forward)
			=> ReorderVectorMenuAuthority.BuildMoveItem(request, XCoreMenuBridge.StripAccelerator(display.Text),
				forward, MoveReferenceItem);

		/// <summary>
		/// The native authority for the reorder-vector menu of the request's row: it answers
		/// Move Left, Move Right and Alphabetical Order from the row itself, so a label menu
		/// carrying that id needs nothing from the hidden command adapter for those leaves.
		/// </summary>
		internal IDetailMenuAuthority CreateReorderVectorAuthority(DetailMenuRequest request)
			=> new ReorderVectorMenuAuthority(request, MoveReferenceItem, ResetReferenceOrder);

		/// <summary>
		/// Moves a reference-vector item one place through the detail edit context and completes
		/// the gesture the way every other detail edit does: validate-then-commit, then one
		/// coalesced re-show, through which the moved item stays current.
		/// </summary>
		internal void MoveReferenceItem(DetailField field, string key, bool forward)
			=> CompleteReferenceEdit(context => context.TryMoveReferenceItem(field, key, forward));

		/// <summary>
		/// Discards a reference-vector row's stored item order (Alphabetical Order) through the
		/// detail edit context, completing the gesture like <see cref="MoveReferenceItem"/>.
		/// </summary>
		internal void ResetReferenceOrder(DetailField field)
			=> CompleteReferenceEdit(context => context.TryResetReferenceOrder(field));

		// One row-menu edit: settle pending edits first (so the edit is its own undo step),
		// stage it, validate-and-commit, then request one coalesced re-show.
		private void CompleteReferenceEdit(Func<IDetailEditContext, bool> stage)
		{
			m_detailEditContext.Settle();
			var context = m_detailEditContext.Current;
			if (context == null || !stage(context))
				return;
			// A failed validation rolls back and never re-shows, so the focus request is made
			// only once the commit is known to have succeeded.
			if (m_detailEditContext.Settle().Count != 0)
				return;
			// The menu took keyboard focus; the re-show hands it to the row's current item.
			m_avaloniaEntryForm?.FocusVectorItemOnNextShow();
			OnAvaloniaDetailEditCompleted(this, EventArgs.Empty);
		}

		string IReferenceItemMenuHost.CurrentTool
			=> m_propertyTable.GetStringProperty("currentContentControl", null);

		void IReferenceItemMenuHost.MoveItem(DetailField field, string key, bool forward)
			=> MoveReferenceItem(field, key, forward);

		// A jump leaves the record, so pending edits settle first, as every link does.
		void IReferenceItemMenuHost.RunJump(Action jump)
		{
			SettleDetailEdits();
			jump();
		}

		void IReferenceItemMenuHost.FilterByAnthroItem(string tool, int anthroItemHvo)
		{
			SettleDetailEdits();
#pragma warning disable 618 // suppress obsolete warning
			m_mediator.PostMessage("FollowLink",
				AnthroItemFilterLink.Create(Cache.ProjectId.Handle, tool, anthroItemHvo));
#pragma warning restore 618
		}

		// Model edits of their own: pending edits settle first (a rollback cancels the mark), so
		// each is its own undo step; the change re-shows through PropChanged.
		void IReferenceItemMenuHost.ToggleSubentryUnderComponent(ILexEntryRef complexFormRef, ICmObject component)
		{
			if (m_detailEditContext.Settle().Count != 0)
				return;
			ComplexFormVisibility.ToggleSubentryUnderComponent(complexFormRef, component,
				xWorksStrings.ksUndoShowSubentryForComponent, xWorksStrings.ksRedoShowSubentryForComponent);
		}

		void IReferenceItemMenuHost.ToggleShowComplexFormIn(ILexEntryRef complexFormRef, ICmObject component)
		{
			if (m_detailEditContext.Settle().Count != 0)
				return;
			ComplexFormVisibility.ToggleShowComplexFormIn(complexFormRef, component,
				xWorksStrings.ksUndoVisibleComplexForm, xWorksStrings.ksRedoVisibleComplexForm);
		}
	}
}
