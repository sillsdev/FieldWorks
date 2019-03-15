// Copyright (c) 2017-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.DetailControls;
using LanguageExplorer.LcmUi;
using SIL.Code;
using SIL.Collections;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;

namespace LanguageExplorer.Areas
{
	/// <summary>
	/// Provides menu adjustments for areas/tools that cross those boundaries, and that areas/tools can be more selective in what to use.
	/// One might think of these as more 'global', but not quite to the level of 'universal' across all areas/tools,
	/// which events are handled by the main window.
	/// </summary>
	internal sealed class PartiallySharedAreaWideMenuHelper : IDisposable
	{
		private MajorFlexComponentParameters _majorFlexComponentParameters;
		private IRecordList _recordList;
		private ISharedEventHandlers _sharedEventHandlers;
		private readonly HashSet<string> _sharedEventKeyNames = new HashSet<string>();

		internal PartiallySharedAreaWideMenuHelper(MajorFlexComponentParameters majorFlexComponentParameters, IRecordList recordList)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
			Guard.AgainstNull(recordList, nameof(recordList));

			_majorFlexComponentParameters = majorFlexComponentParameters;
			_recordList = recordList;

			_sharedEventHandlers = majorFlexComponentParameters.SharedEventHandlers;
			PropertyTable = _majorFlexComponentParameters.FlexComponentParameters.PropertyTable;
			Publisher = _majorFlexComponentParameters.FlexComponentParameters.Publisher;
			Subscriber = _majorFlexComponentParameters.FlexComponentParameters.Subscriber;

#if RANDYTODO
			// TODO: Are these really needed in this class?
#endif
			_sharedEventKeyNames.AddRange(new[]
			{
				AreaServices.InsertSlash,
				AreaServices.InsertEnvironmentBar,
				AreaServices.InsertNaturalClass,
				AreaServices.InsertOptionalItem,
				AreaServices.InsertHashMark,
				AreaServices.ShowEnvironmentError,
				AreaServices.JumpToTool,
				AreaServices.InsertCategory,
				AreaServices.DataTreeDelete,
				AreaServices.CmdAddToLexicon,
				AreaServices.LexiconLookup,
				AreaServices.CmdDeleteSelectedObject,
				AreaServices.DeleteSelectedBrowseViewObject
			});
			foreach (var key in _sharedEventKeyNames)
			{
				switch (key)
				{
					case AreaServices.InsertSlash:
						_sharedEventHandlers.Add(key, Insert_Slash_Clicked);
						break;
					case AreaServices.InsertEnvironmentBar:
						_sharedEventHandlers.Add(key, Insert_Underscore_Clicked);
						break;
					case AreaServices.InsertNaturalClass:
						_sharedEventHandlers.Add(key, Insert_NaturalClass_Clicked);
						break;
					case AreaServices.InsertOptionalItem:
						_sharedEventHandlers.Add(key, Insert_OptionalItem_Clicked);
						break;
					case AreaServices.InsertHashMark:
						_sharedEventHandlers.Add(key, Insert_HashMark_Clicked);
						break;
					case AreaServices.ShowEnvironmentError:
						_sharedEventHandlers.Add(key, ShowEnvironmentError_Clicked);
						break;
					case AreaServices.JumpToTool:
						_sharedEventHandlers.Add(key, JumpToTool_Clicked);
						break;
					case AreaServices.InsertCategory:
						_sharedEventHandlers.Add(key, InsertCategory_Clicked);
						break;
					case AreaServices.DataTreeDelete:
						_sharedEventHandlers.Add(key, DataTreeDelete_Clicked);
						break;
					case AreaServices.CmdAddToLexicon:
						_sharedEventHandlers.Add(key, CmdAddToLexicon_Clicked);
						break;
					case AreaServices.LexiconLookup:
						_sharedEventHandlers.Add(key, LexiconLookup_Clicked);
						break;
					case AreaServices.CmdDeleteSelectedObject:
						_sharedEventHandlers.Add(key, CmdDeleteSelectedObject_Clicked);
						break;
					case AreaServices.DeleteSelectedBrowseViewObject:
						_sharedEventHandlers.Add(key, DeleteSelectedBrowseViewObject_Clicked);
						break;
				}
			}
		}

		/// <summary>
		/// Placement in the IPropertyTableProvider interface lets FwApp call IPropertyTable.DoStuff.
		/// </summary>
		private IPropertyTable PropertyTable { get; }

		/// <summary>
		/// Get the IPublisher.
		/// </summary>
		private IPublisher Publisher { get; }

		/// <summary>
		/// Get the ISubscriber.
		/// </summary>
		private ISubscriber Subscriber { get; }

		#region IDisposable
		private bool _isDisposed;

		~PartiallySharedAreaWideMenuHelper()
		{
			// The base class finalizer is called automatically.
			Dispose(false);
		}

		/// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SuppressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (_isDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				foreach (var key in _sharedEventKeyNames)
				{
					_sharedEventHandlers.Remove(key);
				}
			}
			_majorFlexComponentParameters = null;
			_recordList = null;
			_sharedEventHandlers = null;

			_isDisposed = true;
		}
		#endregion

		/// <summary>
		/// Setup the File->Export menu.
		/// </summary>
		internal void SetupFileExportMenu(ToolUiWidgetParameterObject toolUiWidgetParameterObject)
		{
			// File->Export menu is visible and enabled in this tool.
			Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>> fileMenuItemsForTool;
			if (!toolUiWidgetParameterObject.MenuItemsForTool.TryGetValue(MainMenu.File, out fileMenuItemsForTool))
			{
				fileMenuItemsForTool = new Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>>();
				toolUiWidgetParameterObject.MenuItemsForTool.Add(MainMenu.File, fileMenuItemsForTool);
			}
			fileMenuItemsForTool.Add(Command.CmdExport, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(CommonFileExportMenu_Click, () => CanCmdExport));
		}

		private Tuple<bool, bool> CanCmdExport => new Tuple<bool, bool>(true, true);

		private void CommonFileExportMenu_Click(object sender, EventArgs e)
		{
			// This handles the general case, if nobody else is handling it.
			// Areas/Tools that uses this code:
			// A. lexicon area: all 8 tools
			// B. textsWords area: Analyses, bulkEditWordforms, wordListConcordance
			// C. grammar area: all tools, except grammarSketch, which goes its own way
			// D. lists area: all 27 tools
			if (_recordList.AreCustomFieldsAProblem(new[] { LexEntryTags.kClassId, LexSenseTags.kClassId, LexExampleSentenceTags.kClassId, MoFormTags.kClassId }))
			{
				return;
			}
			using (var dlg = new ExportDialog(_majorFlexComponentParameters.StatusBar))
			{
				dlg.InitializeFlexComponent(_majorFlexComponentParameters.FlexComponentParameters);
				dlg.ShowDialog(PropertyTable.GetValue<Form>(FwUtils.window));
			}
		}

		/// <summary>
		/// Setup the Tools->Configure->CustomFields menu.
		/// </summary>
		internal void SetupToolsCustomFieldsMenu(ToolUiWidgetParameterObject toolUiWidgetParameterObject)
		{
			// Tools->Configure->CustomFields menu is visible and enabled in this tool.
			Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>> toolsMenuItemsForTool;
			if (!toolUiWidgetParameterObject.MenuItemsForTool.TryGetValue(MainMenu.Tools, out toolsMenuItemsForTool))
			{
				toolsMenuItemsForTool = new Dictionary<Command, Tuple<EventHandler, Func<Tuple<bool, bool>>>>();
				toolUiWidgetParameterObject.MenuItemsForTool.Add(MainMenu.Tools, toolsMenuItemsForTool);
			}
			toolsMenuItemsForTool.Add(Command.CmdAddCustomField, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(AddCustomField_Click, () => CanAddCustomField));
		}

		private void DataTreeDelete_Clicked(object sender, EventArgs e)
		{
			HandleDeletion(sender);
		}

		private void CmdDeleteSelectedObject_Clicked(object sender, EventArgs e)
		{
			HandleDeletion(sender);
		}

		private void DeleteSelectedBrowseViewObject_Clicked(object sender, EventArgs e)
		{
			var tag = (IList<object>)((ToolStripMenuItem)sender).Tag;
			((IRecordList)tag[0]).DeleteRecord((string)tag[1], (StatusBarProgressPanel)tag[2]);
		}

		private static void HandleDeletion(object sender)
		{
			SenderTagAsSlice(sender).HandleDeleteCommand();
		}

		private Tuple<bool, bool> CanAddCustomField => new Tuple<bool, bool>(true, true);

		private void AddCustomField_Click(object sender, EventArgs e)
		{
			var activeForm = PropertyTable.GetValue<Form>(FwUtils.window);
			if (SharedBackendServices.AreMultipleApplicationsConnected(PropertyTable.GetValue<LcmCache>(LanguageExplorerConstants.cache)))
			{
				MessageBoxUtils.Show(activeForm, AreaResources.ksCustomFieldsCanNotBeAddedDueToOtherAppsText, AreaResources.ksCustomFieldsCanNotBeAddedDueToOtherAppsCaption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			var locationType = CustomFieldLocationType.Lexicon;
			var areaChoice = PropertyTable.GetValue<string>(AreaServices.AreaChoice);
			switch (areaChoice)
			{
				case AreaServices.LexiconAreaMachineName:
					locationType = CustomFieldLocationType.Lexicon;
					break;
				case AreaServices.NotebookAreaMachineName:
					locationType = CustomFieldLocationType.Notebook;
					break;
				case AreaServices.TextAndWordsAreaMachineName:
					locationType = CustomFieldLocationType.Interlinear;
					break;
			}
			using (var dlg = new AddCustomFieldDlg(PropertyTable, Publisher, locationType))
			{
				if (dlg.ShowCustomFieldWarning(activeForm))
				{
					dlg.ShowDialog(activeForm);
				}
			}
		}

		private void Insert_Slash_Clicked(object sender, EventArgs e)
		{
			UowHelpers.UndoExtension(AreaResources.ksInsertEnvironmentSlash, PropertyTable.GetValue<LcmCache>(LanguageExplorerConstants.cache).ActionHandlerAccessor, () => SenderTagAsIPhEnvSliceCommon(sender).InsertSlash());
		}

		private void Insert_Underscore_Clicked(object sender, EventArgs e)
		{
			UowHelpers.UndoExtension(AreaResources.ksInsertEnvironmentBar, PropertyTable.GetValue<LcmCache>(LanguageExplorerConstants.cache).ActionHandlerAccessor, () => SenderTagAsIPhEnvSliceCommon(sender).InsertEnvironmentBar());
		}

		private void Insert_NaturalClass_Clicked(object sender, EventArgs e)
		{
			UowHelpers.UndoExtension(AreaResources.ksInsertNaturalClass, PropertyTable.GetValue<LcmCache>(LanguageExplorerConstants.cache).ActionHandlerAccessor, () => SenderTagAsIPhEnvSliceCommon(sender).InsertNaturalClass());
		}

		private void Insert_OptionalItem_Clicked(object sender, EventArgs e)
		{
			UowHelpers.UndoExtension(AreaResources.ksInsertOptionalItem, PropertyTable.GetValue<LcmCache>(LanguageExplorerConstants.cache).ActionHandlerAccessor, () => SenderTagAsIPhEnvSliceCommon(sender).InsertOptionalItem());
		}

		private void Insert_HashMark_Clicked(object sender, EventArgs e)
		{
			UowHelpers.UndoExtension(AreaResources.ksInsertWordBoundary, PropertyTable.GetValue<LcmCache>(LanguageExplorerConstants.cache).ActionHandlerAccessor, () => SenderTagAsIPhEnvSliceCommon(sender).InsertHashMark());
		}

		private void ShowEnvironmentError_Clicked(object sender, EventArgs e)
		{
			SenderTagAsIPhEnvSliceCommon(sender).ShowEnvironmentError();
		}

		internal static IPhEnvSliceCommon SenderTagAsIPhEnvSliceCommon(object sender)
		{
			return (IPhEnvSliceCommon)((ToolStripMenuItem)sender).Tag;
		}

		private static Slice SenderTagAsSlice(object sender)
		{
			return ((ToolStripMenuItem)sender).Tag as Slice; // May be null.
		}

		internal static StTextSlice DataTreeCurrentSliceAsStTextSlice(DataTree dataTree)
		{
			return dataTree?.CurrentSlice as StTextSlice; // May be null.
		}

		internal static IPhEnvSliceCommon SliceAsIPhEnvSliceCommon(Slice slice)
		{
			return (IPhEnvSliceCommon)slice;
		}

		internal static void CreateShowEnvironmentErrorMessageMenus(ISharedEventHandlers sharedEventHandlers, Slice slice, List<Tuple<ToolStripMenuItem, EventHandler>> menuItems, ContextMenuStrip contextMenuStrip)
		{
			/*
		      <item command="CmdShowEnvironmentErrorMessage" />
					<command id="CmdShowEnvironmentErrorMessage" label="_Describe Error in Environment" message="ShowEnvironmentError" /> SHARED
			*/
			var menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, sharedEventHandlers.Get(AreaServices.ShowEnvironmentError), LanguageExplorerResources.Describe_Error_in_Environment);
			menu.Enabled = SliceAsIPhEnvSliceCommon(slice).CanShowEnvironmentError;
			menu.Tag = slice;
		}

		internal static void CreateCommonEnvironmentMenus(ISharedEventHandlers sharedEventHandlers, Slice slice, List<Tuple<ToolStripMenuItem, EventHandler>> menuItems, ContextMenuStrip contextMenuStrip)
		{
			if (contextMenuStrip.Items.Count > 0)
			{
				/*
				  <item label="-" translate="do not translate" />
				*/
				ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);
			}

			/*
		      <item command="CmdInsertEnvSlash" />
					<command id="CmdInsertEnvSlash" label="Insert Environment _slash" message="InsertSlash" /> SHARED
			*/
			var menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, sharedEventHandlers.Get(AreaServices.InsertSlash), AreaResources.Insert_Environment_slash);
			menu.Enabled = SliceAsIPhEnvSliceCommon(slice).CanInsertSlash;
			menu.Tag = slice;

			/*
		      <item command="CmdInsertEnvUnderscore" />
					<command id="CmdInsertEnvUnderscore" label="Insert Environment _bar" message="InsertEnvironmentBar" /> SHARED
			*/

			menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, sharedEventHandlers.Get(AreaServices.InsertEnvironmentBar), AreaResources.Insert_Environment_bar);
			menu.Enabled = SliceAsIPhEnvSliceCommon(slice).CanInsertEnvironmentBar;
			menu.Tag = slice;

			/*
		      <item command="CmdInsertEnvNaturalClass" />
					<command id="CmdInsertEnvNaturalClass" label="Insert _Natural Class" message="InsertNaturalClass" /> SHARED
			*/
			menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, sharedEventHandlers.Get(AreaServices.InsertNaturalClass), AreaResources.Insert_Natural_Class);
			menu.Enabled = SliceAsIPhEnvSliceCommon(slice).CanInsertNaturalClass;
			menu.Tag = slice;

			/*
		      <item command="CmdInsertEnvOptionalItem" />
					<command id="CmdInsertEnvOptionalItem" label="Insert _Optional Item" message="InsertOptionalItem" /> SHARED
			*/
			menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, sharedEventHandlers.Get(AreaServices.InsertOptionalItem), AreaResources.Insert_Optional_Item);
			menu.Enabled = SliceAsIPhEnvSliceCommon(slice).CanInsertOptionalItem;
			menu.Tag = slice;

			/*
		      <item command="CmdInsertEnvHashMark" />
					<command id="CmdInsertEnvHashMark" label="Insert _Word Boundary" message="InsertHashMark" /> SHARED
			*/
			menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, sharedEventHandlers.Get(AreaServices.InsertHashMark), AreaResources.Insert_Word_Boundary);
			menu.Enabled = SliceAsIPhEnvSliceCommon(slice).CanInsertHashMark;
			menu.Tag = slice;
		}

		internal static bool CanJumpToTool(string currentToolMachineName, string targetToolMachineNameForJump, LcmCache cache, ICmObject rootObject, ICmObject currentObject, string className)
		{
			if (currentToolMachineName == targetToolMachineNameForJump)
			{
				return (ReferenceEquals(rootObject, currentObject) || currentObject.IsOwnedBy(rootObject));
			}
			if (currentObject is IWfiWordform)
			{
				return _concordanceTools.Contains(targetToolMachineNameForJump);
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

		private static readonly HashSet<string> _concordanceTools = new HashSet<string>
		{
			AreaServices.WordListConcordanceMachineName,
			AreaServices.ConcordanceMachineName
		};

		private static void JumpToTool_Clicked(object sender, EventArgs e)
		{
			var tag = (List<object>)((ToolStripMenuItem)sender).Tag;
			LinkHandler.PublishFollowLinkMessage((IPublisher)tag[0], new FwLinkArgs((string)tag[1], (Guid)tag[2]));
		}

		private void InsertCategory_Clicked(object sender, EventArgs e)
		{
			var tagList = (List<object>)((ToolStripItem)sender).Tag;
			using (var dlg = new MasterCategoryListDlg())
			{
				var recordList = (IRecordList)tagList[1];
				var selectedCategoryOwner = recordList.CurrentObject?.Owner;
				var propertyTable = _majorFlexComponentParameters.FlexComponentParameters.PropertyTable;
				dlg.SetDlginfo((ICmPossibilityList)tagList[0], propertyTable, true, selectedCategoryOwner as IPartOfSpeech);
				dlg.ShowDialog(propertyTable.GetValue<Form>(FwUtils.window));
			}
		}

		internal static void Set_CmdAddToLexicon_State(LcmCache cache, ToolStripItem toolStripItem, IVwSelection selection)
		{
			var enabled = false;
			if (selection != null)
			{
				// Enable the command if the selection exists, we actually have a word, and it's in
				// the default vernacular writing system.
				int ichMin;
				int ichLim;
				int hvoDummy;
				int tagDummy;
				int ws;
				ITsString tss;
				GetWordLimitsOfSelection(selection, out ichMin, out ichLim, out hvoDummy, out tagDummy, out ws, out tss);
				if (ws == 0)
				{
					ws = GetWsFromString(tss, ichMin, ichLim);
				}
				if (ichLim > ichMin && ws == cache.DefaultVernWs)
				{
					enabled = true;
				}
			}

			toolStripItem.Enabled = enabled;
		}

		private void CmdAddToLexicon_Clicked(object sender, EventArgs e)
		{
			var dataTree = (DataTree)((ToolStripItem)sender).Tag;
			var currentSlice = DataTreeCurrentSliceAsStTextSlice(dataTree);
			int ichMin;
			int ichLim;
			int hvoDummy;
			int Dummy;
			int ws;
			ITsString tss;
			GetWordLimitsOfSelection(currentSlice.RootSite.RootBox.Selection, out ichMin, out ichLim, out hvoDummy, out Dummy, out ws, out tss);
			if (ws == 0)
			{
				ws = GetWsFromString(tss, ichMin, ichLim);
			}
			if (ichLim <= ichMin || ws != _majorFlexComponentParameters.LcmCache.DefaultVernWs)
			{
				return;
			}
			var tsb = tss.GetBldr();
			if (ichLim < tsb.Length)
			{
				tsb.Replace(ichLim, tsb.Length, null, null);
			}

			if (ichMin > 0)
			{
				tsb.Replace(0, ichMin, null, null);
			}
			var tssForm = tsb.GetString();
			using (var dlg = new InsertEntryDlg())
			{
				dlg.InitializeFlexComponent(new FlexComponentParameters(PropertyTable, Publisher, Subscriber));
				dlg.SetDlgInfo(_majorFlexComponentParameters.LcmCache, tssForm);
				if (dlg.ShowDialog((Form)_majorFlexComponentParameters.MainWindow) == DialogResult.OK)
				{
					// is there anything special we want to do?
				}
			}
		}

		private static int GetWsFromString(ITsString tss, int ichMin, int ichLim)
		{
			if (tss == null || tss.Length == 0 || ichMin >= ichLim)
			{
				return 0;
			}
			var runMin = tss.get_RunAt(ichMin);
			var runMax = tss.get_RunAt(ichLim - 1);
			var ws = tss.get_WritingSystem(runMin);
			if (runMin == runMax)
			{
				return ws;
			}
			for (var i = runMin + 1; i <= runMax; ++i)
			{
				var wsT = tss.get_WritingSystem(i);
				if (wsT != ws)
				{
					return 0;
				}
			}
			return ws;
		}

		private static void GetWordLimitsOfSelection(IVwSelection sel, out int ichMin, out int ichLim, out int hvo, out int tag, out int ws, out ITsString tss)
		{
			ichMin = ichLim = hvo = tag = ws = 0;
			tss = null;
			IVwSelection wordsel = null;
			if (sel != null)
			{
				var sel2 = sel.EndBeforeAnchor ? sel.EndPoint(true) : sel.EndPoint(false);
				wordsel = sel2?.GrowToWord();
			}
			if (wordsel == null)
			{
				return;
			}

			bool fAssocPrev;
			wordsel.TextSelInfo(false, out tss, out ichMin, out fAssocPrev, out hvo, out tag, out ws);
			wordsel.TextSelInfo(true, out tss, out ichLim, out fAssocPrev, out hvo, out tag, out ws);
		}

		internal bool IsLexiconLookupEnabled(IVwSelection selection)
		{
			if (selection == null)
			{
				return false;
			}
			// Enable the command if the selection exists and we actually have a word.
			int ichMin;
			int ichLim;
			int hvoDummy;
			int tagDummy;
			int wsDummy;
			ITsString tssDummy;
			GetWordLimitsOfSelection(selection, out ichMin, out ichLim, out hvoDummy, out tagDummy, out wsDummy, out tssDummy);
			return ichLim > ichMin;
		}

		private void LexiconLookup_Clicked(object sender, EventArgs e)
		{
			var selection = (IVwSelection)((ToolStripItem)sender).Tag;
			if (selection == null)
			{
				return;
			}
			int ichMin;
			int ichLim;
			int hvo;
			int tag;
			int ws;
			ITsString tss;
			GetWordLimitsOfSelection(selection, out ichMin, out ichLim, out hvo, out tag, out ws, out tss);
			if (ichLim > ichMin)
			{
				LexEntryUi.DisplayOrCreateEntry(_majorFlexComponentParameters.LcmCache, hvo, tag, ws, ichMin, ichLim, PropertyTable.GetValue<IWin32Window>(FwUtils.window), PropertyTable, Publisher, Subscriber, PropertyTable.GetValue<IHelpTopicProvider>(LanguageExplorerConstants.HelpTopicProvider), "UserHelpFile");
			}
		}
	}
}