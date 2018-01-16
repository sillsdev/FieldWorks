// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using SIL.LCModel.Core.WritingSystems;
using LanguageExplorer.Controls.DetailControls.Resources;
using LanguageExplorer.Controls.XMLViews;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Widgets;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;
using SIL.LCModel.Utils;
using SIL.Xml;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	/// Summary description for ViewPropertyItem.
	/// </summary>
	internal class MultiStringSlice : ViewPropertySlice
	{
		private ToolStripMenuItem _writingSystemsMenu;
		private List<ToolStripMenuItem> _writingSystemMenuItems;

		public MultiStringSlice(ICmObject obj, int flid, int ws, int wsOptional, bool forceIncludeEnglish, bool editable, bool spellCheck)
		{
			Control = new LabeledMultiStringView(obj.Hvo, flid, ws, wsOptional, forceIncludeEnglish, editable, spellCheck);
#if _DEBUG
			Control.CheckForIllegalCrossThreadCalls = true;
#endif
			InternalInitialize();
			Reuse(obj, flid);
			var view = View;
			view.InnerView.Display += View_Display;
			view.InnerView.RightMouseClickedEvent += HandleRightMouseClickedEvent;
			view.InnerView.LostFocus += View_LostFocus;
		}

		#region Overrides of Slice
		protected override void PrepareToShowContextMenu()
		{
			base.PrepareToShowContextMenu();

			// Calulate the WS that need to be showed.
			var currentlyAvailableForChecking = new List<string>(WritingSystemOptionsForDisplay.Select(writingSystemDefinition => writingSystemDefinition.DisplayLabel));
			var currentlyCheckedWritingSystems = new List<string>(WritingSystemsSelectedForDisplay.Select(writingSystemDefinition => writingSystemDefinition.DisplayLabel));
			foreach (var wsMenu in _writingSystemMenuItems)
			{
				var tagAsWsDefn = (string)wsMenu.Tag;
				var makeAvailableAndEnabled = currentlyAvailableForChecking.Contains(tagAsWsDefn);
				wsMenu.Available = makeAvailableAndEnabled;
				wsMenu.Enabled = makeAvailableAndEnabled;
				wsMenu.Checked = currentlyCheckedWritingSystems.Contains(tagAsWsDefn);
			}
		}

		protected override void AddSpecialContextMenus(ContextMenuStrip topLevelContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>> menuItems)
		{
			base.AddSpecialContextMenus(topLevelContextMenuStrip, menuItems);

			// Add "Writing Systems" context menu and its sub-menu items.
			_writingSystemsMenu = ToolStripMenuItemFactory.CreateBaseMenuForToolStripMenuItem(topLevelContextMenuStrip, LanguageExplorerResources.WritingSystems);

			ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(menuItems, _writingSystemsMenu, ShowAllWritingSystemsNow_Click, LanguageExplorerResources.ShowAllRightNow);

			// Note: We add all possible individual WS submenus here, and they will be disabled and not visible.
			// The 'PrepareToShowContextMenu' method is called as the main context menu is being shown, and it sort out which menus
			// are relevant for the given context and make them visible and enabled.
			var allWritingSystems = Cache.ServiceLocator.WritingSystems.AllWritingSystems.ToList();
			var sortedWritingSystemDefinitions = new SortedDictionary<string, CoreWritingSystemDefinition>();
			foreach (var ws in allWritingSystems)
			{
				sortedWritingSystemDefinitions.Add(ws.DisplayLabel, ws);
			}
			_writingSystemMenuItems = new List<ToolStripMenuItem>();
			foreach (var wsKvp in sortedWritingSystemDefinitions)
			{
				var wsMenu = ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(menuItems, _writingSystemsMenu, IndividualWritingSystemMenu_Clicked, wsKvp.Key);
				_writingSystemMenuItems.Add(wsMenu);
				wsMenu.Available = false;
				wsMenu.Enabled = false;
				wsMenu.CheckOnClick = true;
				wsMenu.Tag = wsKvp.Value.DisplayLabel;
			}

			ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(menuItems, _writingSystemsMenu, ConfigureWritingSystems_Clicked, LanguageExplorerResources.Configure);
		}

		#endregion

		private void IndividualWritingSystemMenu_Clicked(object sender, EventArgs e)
		{
			var currentlyCheckedCoreWritingSystemDefinitions = new List<CoreWritingSystemDefinition>();
			foreach (var toolStripMenuItem in _writingSystemMenuItems.Where(wsMenu => wsMenu.Checked))
			{
				currentlyCheckedCoreWritingSystemDefinitions.AddRange(Cache.ServiceLocator.WritingSystems.AllWritingSystems.Where(wsDefn => wsDefn.DisplayLabel == (string)toolStripMenuItem.Tag));
			}
			PersistAndRedisplayWssToDisplayForPart(currentlyCheckedCoreWritingSystemDefinitions);
		}

		private void ConfigureWritingSystems_Clicked(object sender, EventArgs eventArgs)
		{
			ReloadWssToDisplayForPart();
			using (var dlg = new ConfigureWritingSystemsDlg(WritingSystemOptionsForDisplay, WritingSystemsSelectedForDisplay, PropertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider")))
			{
				dlg.Text = string.Format(DetailControlsStrings.ksSliceConfigureWssDlgTitle, Label);
				if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
				{
					PersistAndRedisplayWssToDisplayForPart(dlg.SelectedWritingSystems);
				}
			}
		}

		private LabeledMultiStringView View => (LabeledMultiStringView)Control;

		/// <summary>
		/// Get the rootsite. It's important to use this method to get the rootsite, not to
		/// assume that the control is a rootsite, because some classes override and insert
		/// another layer of control, with the root site being a child.
		/// </summary>
		public override RootSite RootSite => View.InnerView;

		/// <summary>
		/// Reset the slice to the state as if it had been constructed with these arguments. (It is going to be
		/// reused for a different record.)
		/// </summary>
		public void Reuse(ICmObject obj, int flid, int ws, int wsOptional, bool forceIncludeEnglish, bool editable, bool spellCheck)
		{
			Label = null; // new slice normally has this
			View.Reuse(obj.Hvo, flid, ws, wsOptional, forceIncludeEnglish, editable, spellCheck);
		}

		public override void FinishInit()
		{
			base.FinishInit();
			View.FinishInit(ConfigurationNode);
		}

		private void View_LostFocus(object sender, EventArgs e)
		{
			DoSideEffects();
		}

		private void DoSideEffects()
		{
			var sideEffectMethod = XmlUtils.GetOptionalAttributeValue(ConfigurationNode, "sideEffectMethod");
			if (string.IsNullOrEmpty(sideEffectMethod))
			{
				return;
			}
			ReflectionHelper.CallMethod(Object, sideEffectMethod, null);
		}

		private void View_Display(object sender, VwEnvEventArgs e)
		{
			XmlVc.ProcessProperties(ConfigurationNode, e.Environment);
		}

		private void HandleRightMouseClickedEvent(SimpleRootSite sender, FwRightMouseClickEventArgs e)
		{
			var sMenu = XmlUtils.GetOptionalAttributeValue(ConfigurationNode, "contextMenu");
			if (string.IsNullOrEmpty(sMenu))
			{
				return;
			}
			e.EventHandled = true;
			e.Selection.Install();
#if RANDYTODO
			var fwMainWnd = PropertyTable.GetValue<IFwMainWnd>("window");
			fwMainWnd.ShowContextMenu(sMenu, new Point(Cursor.Position.X, Cursor.Position.Y), null, null);
#endif
		}

		/// <summary>
		/// Gets a list of the visible writing systems stored in our layout part ref override.
		/// </summary>
		/// <returns></returns>
		private IReadOnlyList<CoreWritingSystemDefinition> VisibleWritingSystems => GetAllVisibleWritingSystems(XmlUtils.GetOptionalAttributeValue(PartRef(), "visibleWritingSystems", null) ?? EncodeWssToDisplayPropertyValue(View.GetWritingSystemOptions(false).ToArray()));

		/// <summary>
		/// convert the given writing systems into a property containing comma-delimited icuLocales.
		/// </summary>
		private static string EncodeWssToDisplayPropertyValue(IEnumerable<CoreWritingSystemDefinition> wss)
		{
			var wsIds = wss.Select(ws => ws.Id).ToArray();
			return wsIds.Length == 0 ? string.Empty : string.Join(",", wsIds);
		}

		/// <summary>
		/// Get the writing systems we should actually display right now. That is, from the ones
		/// that are currently possible, select any we've previously configured to show.
		/// </summary>
		private IReadOnlyList<CoreWritingSystemDefinition> GetAllVisibleWritingSystems(string singlePropertySequenceValue)
		{
			var wsIdSet = new HashSet<string>(singlePropertySequenceValue.Split(','));
			return WritingSystemOptionsForDisplay.Where(ws => wsIdSet.Contains(ws.Id)).ToList();
		}

		public override void Install(DataTree parentDataTree)
		{
			base.Install(parentDataTree);
			// setup the visible writing systems for our control
			// (We should have called MakeRoot on our control by now)
			WritingSystemsSelectedForDisplay = VisibleWritingSystems;
		}

		/// <summary>
		/// Make a selection in the specified writing system at the specified character offset.
		/// Note: selecting other than the first writing system is not yet implemented.
		/// </summary>
		/// <param name="ws"></param>
		/// <param name="ich"></param>
		public void SelectAt(int ws, int ich)
		{
			CheckDisposed();
			((LabeledMultiStringView) Control).SelectAt(ws, ich);
		}

		/// <summary>
		/// Get the writing systems that are available for displaying on our slice.
		/// </summary>
		private IReadOnlyList<CoreWritingSystemDefinition> WritingSystemOptionsForDisplay => ((LabeledMultiStringView)Control).WritingSystemOptions;

		/// <summary>
		/// Get/Set the writing systems selected to be displayed for this kind of slice.
		/// </summary>
		internal IEnumerable<CoreWritingSystemDefinition> WritingSystemsSelectedForDisplay
		{
			get
			{
				// If we're not initialized enough to know what ones are being displayed,
				// get the default we expect to be initialized to.
				if (Control == null)
				{
					return VisibleWritingSystems;
				}
				var result = ((LabeledMultiStringView)Control).WritingSystemsToDisplay;
				return result.Count == 0 ? VisibleWritingSystems : result;
			}
			set
			{
				var labeledMultiStringView = (LabeledMultiStringView)Control;
				if (labeledMultiStringView.WritingSystemsToDisplay?.SequenceEqual(value) ?? false)
				{
					return; // no change.
				}
				labeledMultiStringView.WritingSystemsToDisplay = value.ToList();
				labeledMultiStringView.RefreshDisplay();
			}
		}

		/// <summary>
		/// Show all the available writing system fields for this slice, while it is the "current" slice
		/// on the data tree. When it is no longer current, we'll reload/refresh the slice in SetCurrentState().
		/// </summary>
		private void ShowAllWritingSystemsNow_Click(object sender, EventArgs eventArgs)
		{
			SetWssToDisplayForPart(WritingSystemOptionsForDisplay);
		}

		/// <summary>
		/// when our slice moves from being current to not being current,
		/// we want to redisplay the writing systems configured for that slice,
		/// since the user may have selected "Show all for now" which is only
		/// valid while the slice is current.
		/// </summary>
		/// <param name="isCurrent"></param>
		public override void SetCurrentState(bool isCurrent)
		{
			if (!isCurrent)
			{
				ReloadWssToDisplayForPart();
				DoSideEffects();
			}
			base.SetCurrentState(isCurrent);
		}

		/// <summary>
		/// reload the WssToDisplay if we haven't defined any, since
		/// OnDataTreeWritingSystemsShowAll may have temporary masked them.
		/// </summary>
		private void ReloadWssToDisplayForPart()
		{
			if (WritingSystemsSelectedForDisplay == null)
			{
				SetWssToDisplayForPart(VisibleWritingSystems);
			}
		}

		#region Overrides of Slice and/or ViewSlice

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				var view = View;
				view.InnerView.Display -= View_Display;
				view.InnerView.RightMouseClickedEvent -= HandleRightMouseClickedEvent;
				view.InnerView.LostFocus -= View_LostFocus;
				foreach (var wsMenu in _writingSystemMenuItems)
				{
					if (wsMenu.Text == LanguageExplorerResources.ShowAllRightNow)
					{
						wsMenu.Click -= ShowAllWritingSystemsNow_Click;
					}
					else if (wsMenu.Text == LanguageExplorerResources.Configure)
					{
						wsMenu.Click -= ConfigureWritingSystems_Clicked;
					}
					else
					{
						wsMenu.Click -= IndividualWritingSystemMenu_Clicked;
						wsMenu.Tag = null;
					}
					wsMenu.Dispose();
				}
				_writingSystemMenuItems.Clear();
				_writingSystemsMenu.Dispose();
			}
			_writingSystemsMenu = null;
			_writingSystemMenuItems = null;

			base.Dispose(disposing);
		}

		#endregion

		private void PersistAndRedisplayWssToDisplayForPart(IEnumerable<CoreWritingSystemDefinition> wssToDisplayNewValue)
		{
			var singlePropertySequenceValue = EncodeWssToDisplayPropertyValue(wssToDisplayNewValue);
			ReplacePartWithNewAttribute("visibleWritingSystems", singlePropertySequenceValue);
			var wssToDisplay = GetAllVisibleWritingSystems(singlePropertySequenceValue).ToList();
			if (Key.Length > 0)
			{
				var lastKey = Key[Key.Length - 1] as XElement;
				// This is a horrible kludge to implement LT-9620 and catch the fact that we are changing the list
				// of current pronunciation writing systems, and update the database.
				if (lastKey != null && XmlUtils.GetOptionalAttributeValue(lastKey, "menu") == "mnuDataTree-Pronunciation")
				{
					UpdatePronunciationWritingSystems(wssToDisplay);
				}
			}
			SetWssToDisplayForPart(wssToDisplay);
		}

		/// <summary>
		/// Get the language project's list of pronunciation writing systems into sync with the supplied list.
		/// </summary>
		private void UpdatePronunciationWritingSystems(IReadOnlyList<CoreWritingSystemDefinition> wssToDisplay)
		{
			if (wssToDisplay.Count != m_cache.ServiceLocator.WritingSystems.CurrentPronunciationWritingSystems.Count
				|| !m_cache.ServiceLocator.WritingSystems.CurrentPronunciationWritingSystems.SequenceEqual(wssToDisplay))
			{
				NonUndoableUnitOfWorkHelper.Do(m_cache.ServiceLocator.GetInstance<IActionHandler>(), () =>
				{
					m_cache.ServiceLocator.WritingSystems.CurrentPronunciationWritingSystems.Clear();
					foreach (var ws in wssToDisplay)
					{
						m_cache.ServiceLocator.WritingSystems.CurrentPronunciationWritingSystems.Add(ws);
					}
				});
			}
		}

		/// <summary>
		/// Go through all the data tree slices, finding the slices that refer to the same part as this slice
		/// setting them to the same writing systems to display and redisplaying their views.
		/// </summary>
		private void SetWssToDisplayForPart(IReadOnlyList<CoreWritingSystemDefinition> wssToDisplay)
		{
			var ourPart = PartRef();
			var writingSystemsToDisplay = wssToDisplay?.ToList();
			foreach (var slice in ContainingDataTree.Slices.Where(slice => slice.PartRef() == ourPart))
			{
				var msView = (LabeledMultiStringView)slice.Control;
				msView.WritingSystemsToDisplay = writingSystemsToDisplay;
				msView.RefreshDisplay();
			}
		}
	}
}