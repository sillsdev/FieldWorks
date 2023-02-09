// Copyright (c) 2017-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.DetailControls;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Areas
{
	/// <summary>
	/// Area level services
	/// </summary>
	internal static class AreaServices
	{
		#region LanguageExplorer.DictionaryConfiguration.ImageHolder smallCommandImages image constants
		internal const int MoveUpIndex = 12;
		internal const int MoveRightIndex = 13;
		internal const int MoveDownIndex = 14;
		internal const int MoveLeftIndex = 15;
		#endregion LanguageExplorer.DictionaryConfiguration.ImageHolder smallCommandImages image constants

		#region Random strings
		internal const string Default = "Default";
		internal const string ShortName = "ShortName";
		internal const string OwningField = "field";
		internal const string ClassName = "className";
		internal const string OwnerClassName = "ownerClassName";
		internal const string BaseUowMessage = "baseUowMessage";
		internal const string LeftPanelMenuId = "left";
		internal const string RightPanelMenuId = "right";
		internal const string MoveUp = "MoveUp";
		internal const string MoveDown = "MoveDown";
		internal const string Promote = "Promote";
		internal const string List_Item = "List Item";
		internal const string Subitem = "Subitem";
		internal const string Duplicate = "Duplicate";
		internal const string EntriesOrChildren = "entriesOrChildren";
		internal const string ConcOccurrences = "ConcOccurrences";
		#endregion Random strings

		/// <summary>
		/// Handle the provided import dialog.
		/// </summary>
		internal static void HandleDlg(Form importDlg, LcmCache cache, IFlexApp flexApp, IFwMainWnd mainWindow, IPropertyTable propertyTable, IPublisher publisher)
		{
			var oldWsUser = cache.WritingSystemFactory.UserWs;
			((IFwExtension)importDlg).Init(cache, propertyTable, publisher);
			if (importDlg.ShowDialog((Form)mainWindow) != DialogResult.OK)
			{
				return;
			}
			switch (importDlg)
			{
				// NB: Some clients are not any of the types that are checked below, which is fine. That means nothing else is done here.
				case IFormReplacementNeeded _ when oldWsUser != cache.WritingSystemFactory.UserWs:
					flexApp.ReplaceMainWindow(mainWindow);
					break;
				case IImportForm _:
					// Make everything we've imported visible.
					mainWindow.RefreshAllViews();
					break;
			}
		}

		public static bool UpdateCachedObjects(LcmCache cache, FieldDescription fd)
		{
			// We need to find every instance of a reference from this flid to that custom list and delete it!
			// I can't figure out any other way of ensuring that EnsureCompleteIncomingRefs doesn't try to refer
			// to a non-existent flid at some point.
			var owningListGuid = fd.ListRootId;
			if (owningListGuid == Guid.Empty)
			{
				return false;
			}
			var list = cache.ServiceLocator.GetInstance<ICmPossibilityListRepository>().GetObject(owningListGuid);
			// This is only a problem for fields referencing a custom list
			if (list.Owner != null)
			{
				// Not a custom list.
				return false;
			}
			bool changed;
			var type = fd.Type;
			var objRepo = cache.ServiceLocator.GetInstance<ICmObjectRepository>();
			var objClass = fd.Class;
			var flid = fd.Id;
			var ddbf = cache.DomainDataByFlid;
			switch (type)
			{
				case CellarPropertyType.ReferenceSequence: // drop through
				case CellarPropertyType.ReferenceCollection:
					// Handle multiple reference fields
					// Is there a way to do this in LINQ without repeating the get_VecSize call?
					var tupleList = new List<Tuple<int, int>>();
					tupleList.AddRange(objRepo.AllInstances(objClass).Where(obj => ddbf.get_VecSize(obj.Hvo, flid) > 0)
						.Select(obj => new Tuple<int, int>(obj.Hvo, ddbf.get_VecSize(obj.Hvo, flid))));
					NonUndoableUnitOfWorkHelper.Do(cache.ActionHandlerAccessor, () =>
					{
						foreach (var partResult in tupleList)
						{
							ddbf.Replace(partResult.Item1, flid, 0, partResult.Item2, null, 0);
						}
					});
					changed = tupleList.Any();
					break;
				case CellarPropertyType.ReferenceAtomic:
					// Handle atomic reference fields
					// If there's a value for (Hvo, flid), nullify it!
					var objsWithDataThisFlid = new List<int>();
					objsWithDataThisFlid.AddRange(objRepo.AllInstances(objClass).Where(obj => ddbf.get_ObjectProp(obj.Hvo, flid) > 0).Select(obj => obj.Hvo));
					// Delete these references
					NonUndoableUnitOfWorkHelper.Do(cache.ActionHandlerAccessor, () =>
					{
						foreach (var hvo in objsWithDataThisFlid)
						{
							ddbf.SetObjProp(hvo, flid, LcmCache.kNullHvo);
						}
					});
					changed = objsWithDataThisFlid.Any();
					break;
				default:
					changed = false;
					break;
			}
			return changed;
		}

		internal static void ResetMainPossibilityInsertUiWidgetsText(UiWidgetController uiWidgetController, string newText, string newToolTipText = null)
		{
			ResetInsertUiWidgetsText(uiWidgetController.InsertMenuDictionary[Command.CmdInsertPossibility], newText,
				uiWidgetController.InsertToolBarDictionary[Command.CmdInsertPossibility], String.IsNullOrWhiteSpace(newToolTipText) ? newText : newToolTipText);
		}

		internal static void ResetSubitemPossibilityInsertUiWidgetsText(UiWidgetController uiWidgetController, string newText, string newToolTipText = null)
		{
			ResetInsertUiWidgetsText(uiWidgetController.InsertMenuDictionary[Command.CmdDataTree_Insert_Possibility], newText,
				uiWidgetController.InsertToolBarDictionary[Command.CmdDataTree_Insert_Possibility], String.IsNullOrWhiteSpace(newToolTipText) ? newText : newToolTipText);
		}

		internal static void ResetMainPossibilityDuplicateUiWidgetsText(UiWidgetController uiWidgetController, string newText, string newToolTipText = null)
		{
			ResetInsertUiWidgetsText(uiWidgetController.InsertMenuDictionary[Command.CmdDuplicatePossibility], newText,
				uiWidgetController.InsertToolBarDictionary[Command.CmdDuplicatePossibility], String.IsNullOrWhiteSpace(newToolTipText) ? newText : newToolTipText);
		}

		private static void ResetInsertUiWidgetsText(ToolStripItem menu, string newText, ToolStripItem toolBarButton, string newToolTipText)
		{
			menu.Text = newText;
			toolBarButton.ToolTipText = newToolTipText;
		}

		/// <summary>
		/// See if a menu is visible/enabled that moves items down in an owning property.
		/// </summary>
		internal static bool CanMoveDownObjectInOwningSequence(DataTree dataTree, LcmCache cache, out bool visible)
		{
			visible = false;
			bool enabled;
			var type = CellarPropertyType.ReferenceAtomic;
			var sliceObject = dataTree.CurrentSlice.MyCmObject;
			var owningFlid = sliceObject.OwningFlid;
			if (owningFlid > 0)
			{
				type = (CellarPropertyType)cache.DomainDataByFlid.MetaDataCache.GetFieldType(owningFlid);
			}
			if (type != CellarPropertyType.OwningSequence && type != CellarPropertyType.ReferenceSequence)
			{
				visible = false;
				return false;
			}
			var owningObject = sliceObject.Owner;
			var chvo = cache.DomainDataByFlid.get_VecSize(owningObject.Hvo, owningFlid);
			if (chvo < 2)
			{
				enabled = false;
			}
			else
			{
				var hvo = cache.DomainDataByFlid.get_VecItem(owningObject.Hvo, owningFlid, 0);
				enabled = sliceObject.Hvo != hvo;
				// if the first LexEntryRef in LexEntry.EntryRefs is a complex form, and the
				// slice displays the second LexEntryRef in the sequence, then we can't move it
				// up, since the first slot is reserved for the complex form.
				if (enabled && owningFlid == LexEntryTags.kflidEntryRefs && cache.DomainDataByFlid.get_VecSize(hvo, LexEntryRefTags.kflidComplexEntryTypes) > 0)
				{
					enabled = sliceObject.Hvo != cache.DomainDataByFlid.get_VecItem(owningObject.Hvo, owningFlid, 1);
				}
				else
				{
					var sliceObjIdx = cache.DomainDataByFlid.GetObjIndex(owningObject.Hvo, owningFlid, sliceObject.Hvo);
					enabled = sliceObjIdx < chvo - 1;
				}
			}
			visible = true;
			return enabled;
		}

		/// <summary>
		/// See if a menu is visible/enabled that moves items up in an owning property.
		/// </summary>
		internal static bool CanMoveUpObjectInOwningSequence(DataTree dataTree, LcmCache cache, out bool visible)
		{
			visible = false;
			bool enabled;
			var type = CellarPropertyType.ReferenceAtomic;
			var sliceObject = dataTree.CurrentSlice.MyCmObject;
			var owningFlid = sliceObject.OwningFlid;
			if (owningFlid > 0)
			{
				type = (CellarPropertyType)cache.DomainDataByFlid.MetaDataCache.GetFieldType(owningFlid);
			}
			if (type != CellarPropertyType.OwningSequence && type != CellarPropertyType.ReferenceSequence)
			{
				return false;
			}
			var owningObject = sliceObject.Owner;
			var chvo = cache.DomainDataByFlid.get_VecSize(owningObject.Hvo, owningFlid);
			if (chvo < 2)
			{
				enabled = false;
			}
			else
			{
				var hvo = cache.DomainDataByFlid.get_VecItem(owningObject.Hvo, owningFlid, 0);
				enabled = sliceObject.Hvo != hvo;
				if (enabled && owningFlid == LexEntryTags.kflidEntryRefs && cache.DomainDataByFlid.get_VecSize(hvo, LexEntryRefTags.kflidComplexEntryTypes) > 0)
				{
					// if the first LexEntryRef in LexEntry.EntryRefs is a complex form, and the
					// slice displays the second LexEntryRef in the sequence, then we can't move it
					// up, since the first slot is reserved for the complex form.
					enabled = sliceObject.Hvo != cache.DomainDataByFlid.get_VecItem(owningObject.Hvo, owningFlid, 1);
				}
				else
				{
					var sliceObjIdx = cache.DomainDataByFlid.GetObjIndex(owningObject.Hvo, owningFlid, sliceObject.Hvo);
					enabled = sliceObjIdx > 0;
				}
			}
			visible = true;

			return enabled;
		}

		internal static void CreateDeleteMenuItem(List<Tuple<ToolStripMenuItem, EventHandler>> menuItems, ContextMenuStrip contextMenuStrip, ISlice slice, string menuText, EventHandler deleteEventHandler)
		{
			var menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, deleteEventHandler, menuText, image: LanguageExplorerResources.Delete);
			menu.Enabled = !slice.IsGhostSlice && slice.CanDeleteNow;
			if (!menu.Enabled)
			{
				menu.Text = $"{menuText} {StringTable.Table.GetString("(cannot delete this)")}";
			}
			menu.ImageTransparentColor = Color.Magenta;
			menu.Tag = slice;
		}

		internal static Dictionary<string, string> PopulateForMainItemInsert(ICmPossibilityList owningList, ICmPossibility currentPossibility, string baseUowMessage)
		{
			Guard.AgainstNull(owningList, nameof(owningList));
			// The list may be empty, so 'currentPossibility' may be null.
			var mdc = owningList.Cache.GetManagedMetaDataCache();
			var owningPossibility = currentPossibility?.OwningPossibility;
			string className;
			string ownerClassName;
			if (owningPossibility == null)
			{
				className = owningList.ClassName;
				ownerClassName = mdc.GetFieldName(CmPossibilityListTags.kflidPossibilities);
			}
			else
			{
				className = owningPossibility.ClassName;
				ownerClassName = mdc.GetFieldName(CmPossibilityTags.kflidSubPossibilities);
			}
			// Top level newbies are of the class specified in the list,
			// even for lists that allow for certain newbies to be of some other class, such as the variant entry ref type list.
			return CreateSharedInsertDictionary(mdc.GetClassName(owningList.ItemClsid), className, ownerClassName, baseUowMessage);
		}

		internal static Dictionary<string, string> PopulateForSubitemInsert(ICmPossibilityList owningList, ICmPossibility owningPossibility, string baseUowMessage)
		{
			// There has to be a list that ultimately owns a possibility.
			Guard.AgainstNull(owningList, nameof(owningList));

			var mdc = owningList.Cache.GetManagedMetaDataCache();
			var className = owningPossibility == null ? mdc.GetClassName(owningList.ItemClsid) : owningPossibility.ClassName;
			var ownerClassName = className;
			return CreateSharedInsertDictionary(className, ownerClassName, mdc.GetFieldName(CmPossibilityTags.kflidSubPossibilities), baseUowMessage);
		}

		private static Dictionary<string, string> CreateSharedInsertDictionary(string className, string ownerClassName, string owningFieldName, string baseUowMessage)
		{
			return new Dictionary<string, string>
			{
				{ ClassName, className },
				{ OwnerClassName, ownerClassName },
				{ OwningField, owningFieldName },
				{ BaseUowMessage, baseUowMessage }
			};
		}

		internal static bool CanJumpToTool(string currentToolMachineName, string targetToolMachineNameForJump, LcmCache cache, ICmObject rootObject, ICmObject currentObject, string className)
		{
			if (currentToolMachineName == targetToolMachineNameForJump)
			{
				return (ReferenceEquals(rootObject, currentObject) || currentObject.IsOwnedBy(rootObject));
			}
			if (currentObject is IWfiWordform)
			{
				var concordanceTools = new HashSet<string>
				{
					LanguageExplorerConstants.WordListConcordanceMachineName, LanguageExplorerConstants.ConcordanceMachineName
				};
				return concordanceTools.Contains(targetToolMachineNameForJump);
			}
			// Do it the hard way.
			var specifiedClsid = 0;
			var mdc = cache.GetManagedMetaDataCache();
			if (mdc.ClassExists(className)) // otherwise is is a 'magic' class name treated specially in other OnDisplays.
			{
				specifiedClsid = mdc.GetClassId(className);
			}
			if (specifiedClsid == 0)
			{
				// Not visible or enabled.
				return false; // a special magic class id, only enabled explicitly.
			}
			if (currentObject.ClassID == specifiedClsid)
			{
				// Visible & enabled.
				return true;
			}
			// Visible & enabled are the same at this point.
			return cache.DomainDataByFlid.MetaDataCache.GetBaseClsId(currentObject.ClassID) == specifiedClsid;
		}

		/// <summary>
		/// It will add the menu (and optional separator) only if the menu will be both visible and enabled.
		/// </summary>
		internal static void ConditionallyAddJumpToToolMenuItem(ContextMenuStrip contextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>> menuItems, LcmCache cache, IPublisher publisher,
			ICmObject rootObject, ICmObject selectedObject, EventHandler eventHandler, string currentToolMachineName, string targetToolName, ref bool wantSeparator, string className,
			string menuLabel, int separatorInsertLocation = 0)
		{
			var visibleAndEnabled = CanJumpToTool(currentToolMachineName, targetToolName, cache, rootObject, selectedObject, className);
			if (visibleAndEnabled)
			{
				if (wantSeparator)
				{
					ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip, separatorInsertLocation);
					wantSeparator = false;
				}
				var menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, eventHandler, menuLabel);
				menu.Tag = new List<object> { publisher, targetToolName, selectedObject };
			}
		}

		internal static void MoveUpObjectInOwningSequence(LcmCache cache, ISlice slice)
		{
			var owningObject = slice.MyCmObject.Owner;
			var owningFlid = slice.MyCmObject.OwningFlid;
			var indexInOwningProperty = cache.DomainDataByFlid.GetObjIndex(owningObject.Hvo, owningFlid, slice.MyCmObject.Hvo);
			if (indexInOwningProperty > 0)
			{
				// The slice might be invalidated by the MoveOwningSequence, so we get its
				// values first.  See LT-6670.
				// We found it in the sequence, and it isn't already the first.
				UndoableUnitOfWorkHelper.Do(AreaResources.UndoMoveItem, AreaResources.RedoMoveItem, cache.ActionHandlerAccessor,
					() => cache.DomainDataByFlid.MoveOwnSeq(owningObject.Hvo, (int)owningFlid, indexInOwningProperty, indexInOwningProperty, owningObject.Hvo, owningFlid, indexInOwningProperty - 1));
			}
		}

		internal static void MoveDownObjectInOwningSequence(LcmCache cache, ISlice slice)
		{
			var owningObject = slice.MyCmObject.Owner;
			var owningFlid = slice.MyCmObject.OwningFlid;
			var count = cache.DomainDataByFlid.get_VecSize(owningObject.Hvo, owningFlid);
			var indexInOwningProperty = cache.DomainDataByFlid.GetObjIndex(owningObject.Hvo, owningFlid, slice.MyCmObject.Hvo);
			if (indexInOwningProperty >= 0 && indexInOwningProperty + 1 < count)
			{
				// The slice might be invalidated by the MoveOwningSequence, so we get its
				// values first.  See LT-6670.
				// We found it in the sequence, and it isn't already the last.
				// Quoting from VwOleDbDa.cpp, "Insert the selected records before the
				// DstStart object".  This means we need + 2 instead of + 1 for the
				// new location.
				UndoableUnitOfWorkHelper.Do(AreaResources.UndoMoveItem, AreaResources.RedoMoveItem, cache.ActionHandlerAccessor,
					() => cache.DomainDataByFlid.MoveOwnSeq(owningObject.Hvo, owningFlid, indexInOwningProperty, indexInOwningProperty, owningObject.Hvo, owningFlid, indexInOwningProperty + 2));
			}
		}

		internal static string GetMergeMenuText(bool enabled, string baseText)
		{
			return enabled ? baseText : $"{baseText} {StringTable.Table.GetString("(cannot merge this)")}";
		}
	}
}