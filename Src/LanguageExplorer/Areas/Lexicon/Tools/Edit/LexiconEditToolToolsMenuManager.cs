// Copyright (c) 2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Areas.Lexicon.DictionaryConfiguration;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.XMLViews;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
	/// <summary>
	/// Implementation that supports the addition(s) to FLEx's main Tools menu for the Lexicon Edit tool.
	/// </summary>
	internal sealed class LexiconEditToolToolsMenuManager : IToolUiWidgetManager
	{
		private IRecordList MyRecordList { get; set; }
		private Dictionary<string, EventHandler> _sharedEventHandlers;
		private FlexComponentParameters _flexComponentParameters;
		private IPublisher _publisher;
		private LcmCache _cache;
		private IFwMainWnd _mainWnd;
		private LexiconAreaMenuHelper _lexiconAreaMenuHelper;
		private BrowseViewer _browseViewer;
		private ToolStripMenuItem _toolsMenu;
		private List<Tuple<ToolStripMenuItem, EventHandler>> _newToolsMenusAndHandlers = new List<Tuple<ToolStripMenuItem, EventHandler>>();
		private ToolStripSeparator _toolMenuToolStripSeparator;
		private ToolStripMenuItem _toolsConfigureMenu;
		private List<Tuple<ToolStripMenuItem, EventHandler>> _newToolsConfigurationMenusAndHandlers = new List<Tuple<ToolStripMenuItem, EventHandler>>();

		public LexiconEditToolToolsMenuManager(LexiconAreaMenuHelper lexiconAreaMenuHelper, BrowseViewer browseViewer)
		{
			_lexiconAreaMenuHelper = lexiconAreaMenuHelper;
			_browseViewer = browseViewer;
		}

		#region IToolUiWidgetManager

		/// <inheritdoc />
		void IToolUiWidgetManager.Initialize(MajorFlexComponentParameters majorFlexComponentParameters, Dictionary<string, EventHandler> sharedEventHandlers, IRecordList recordList)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
			Guard.AgainstNull(sharedEventHandlers, nameof(sharedEventHandlers));
			Guard.AgainstNull(recordList, nameof(recordList));

			_flexComponentParameters = majorFlexComponentParameters.FlexComponentParameters;
			_publisher = majorFlexComponentParameters.FlexComponentParameters.Publisher;
			_cache = majorFlexComponentParameters.LcmCache;
			_mainWnd = majorFlexComponentParameters.MainWindow;
			_sharedEventHandlers = sharedEventHandlers;
			_sharedEventHandlers.Add(LexiconEditToolConstants.CmdMergeEntry, Merge_With_Entry_Clicked);
			MyRecordList = recordList;

			var insertIndex = -1;

			// <command id="CmdConfigureDictionary" label="Configure {0}" message="ConfigureDictionary"/>
			// <item label="{0}" command="CmdConfigureDictionary" defaultVisible="false"/>
			_toolsConfigureMenu = MenuServices.GetToolsConfigureMenu(majorFlexComponentParameters.MenuStrip);
			ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(_newToolsConfigurationMenusAndHandlers, _toolsConfigureMenu, Tools_Configure_Dictionary_Clicked, AreaResources.ConfigureDictionary, insertIndex: ++insertIndex);

			// <item command="CmdConfigureColumns" defaultVisible="false" />
			_lexiconAreaMenuHelper.MyAreaWideMenuHelper.SetupToolsConfigureColumnsMenu(_browseViewer, ++insertIndex);

			// <item command="CmdMergeEntry" defaultVisible="false"/>
			// First add separator.
			insertIndex = 0;
			_toolsMenu = MenuServices.GetToolsMenu(majorFlexComponentParameters.MenuStrip);
			_toolMenuToolStripSeparator = ToolStripMenuItemFactory.CreateToolStripSeparatorForToolStripMenuItem(_toolsMenu, ++insertIndex);
			ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(_newToolsMenusAndHandlers, _toolsMenu, Merge_With_Entry_Clicked, LexiconResources.MergeWithEntry, LexiconResources.Merge_With_Entry_Tooltip, insertIndex: ++insertIndex);
		}

		#endregion

		#region IDisposable

		private bool _isDisposed;

		~LexiconEditToolToolsMenuManager()
		{
			// The base class finalizer is called automatically.
			Dispose(false);
		}

		/// <inheritdoc />
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
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
				// No need to do it more than once.
				return;
			}

			if (disposing)
			{
				foreach (var menuTuple in _newToolsConfigurationMenusAndHandlers)
				{
					menuTuple.Item1.Click -= menuTuple.Item2;
					_toolsConfigureMenu.DropDownItems.Remove(menuTuple.Item1);
					menuTuple.Item1.Dispose();
				}
				_newToolsConfigurationMenusAndHandlers.Clear();

				foreach (var menuTuple in _newToolsMenusAndHandlers)
				{
					menuTuple.Item1.Click -= menuTuple.Item2;
					_toolsMenu.DropDownItems.Remove(menuTuple.Item1);
					menuTuple.Item1.Dispose();
				}
				_newToolsMenusAndHandlers.Clear();
				_toolsMenu.DropDownItems.Remove(_toolMenuToolStripSeparator);
				_toolMenuToolStripSeparator.Dispose();
			}
			MyRecordList = null;
			_sharedEventHandlers = null;
			_flexComponentParameters = null;
			_publisher = null;
			_cache = null;
			_mainWnd = null;
			_lexiconAreaMenuHelper = null;
			_browseViewer = null;
			_toolsMenu = null;
			_newToolsMenusAndHandlers = null;
			_toolMenuToolStripSeparator = null;
			_toolsConfigureMenu = null;
			_newToolsConfigurationMenusAndHandlers = null;

			_isDisposed = true;
		}

		#endregion

		private void Tools_Configure_Dictionary_Clicked(object sender, EventArgs e)
		{
			if (DictionaryConfigurationDlg.ShowDialog(_flexComponentParameters, (Form)_mainWnd, MyRecordList.CurrentObject, "khtpConfigureDictionary", LanguageExplorerResources.Dictionary))
			{
				_mainWnd.RefreshAllViews();
			}
		}

		private void Merge_With_Entry_Clicked(object sender, EventArgs e)
		{
			var currentObject = MyRecordList.CurrentObject;
			if (currentObject == null)
			{
				return; // should never happen, but nothing we can do if it does!
			}

			var currentEntry = currentObject as ILexEntry ?? currentObject.OwnerOfClass(LexEntryTags.kClassId) as ILexEntry;
			if (currentEntry == null)
			{
				return;
			}

			using (var dlg = new MergeEntryDlg())
			{
				dlg.InitializeFlexComponent(_flexComponentParameters);
				// <parameters title="Merge Entry" formlabel="_Find:" okbuttonlabel="_Merge"/>
				dlg.SetDlgInfo(_cache, XElement.Parse(LexiconResources.MatchingEntriesParameters), currentEntry, LexiconResources.ksMergeEntry, FwUtils.ReplaceUnderlineWithAmpersand(LexiconResources.ks_Find), FwUtils.ReplaceUnderlineWithAmpersand(LexiconResources.ks_Merge));
				if (dlg.ShowDialog((Form)_mainWnd) != DialogResult.OK)
				{
					return;
				}

				var survivor = (ILexEntry)dlg.SelectedObject;
				Debug.Assert(survivor != currentEntry);
				UndoableUnitOfWorkHelper.Do(LexiconResources.ksUndoMergeEntry, LexiconResources.ksRedoMergeEntry, _cache.ActionHandlerAccessor, () =>
				{
					survivor.MergeObject(currentEntry, true);
					survivor.DateModified = DateTime.Now;
				});
				MessageBox.Show((Form)_mainWnd, LexiconResources.ksEntriesHaveBeenMerged, LexiconResources.ksMergeReport, MessageBoxButtons.OK, MessageBoxIcon.Information);
				LinkHandler.JumpToTool(_publisher, new FwLinkArgs(null, survivor.Guid));
			}
		}
	}
}