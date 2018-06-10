// Copyright (c) 2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.DetailControls;
using LanguageExplorer.LcmUi;
using SIL.Code;
using SIL.LCModel;

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
	/// <summary>
	/// Implementation that supports the addition(s) to the DataTree's context menus and hotlinks for a LexSense, and objects it owns, in the Lexicon Edit tool.
	/// </summary>
	internal sealed class LexiconEditToolDataTreeStackLexSenseManager : IToolUiWidgetManager
	{
		private const string mnuDataTree_Sense_Hotlinks = "mnuDataTree-Sense-Hotlinks";
		private Dictionary<string, EventHandler> _sharedEventHandlers;
		private IRecordList MyRecordList { get; set; }
		private DataTreeStackContextMenuFactory MyDataTreeStackContextMenuFactory { get; }
		private DataTree MyDataTree { get; }
		private LcmCache _cache;

		internal LexiconEditToolDataTreeStackLexSenseManager(DataTree dataTree)
		{
			Guard.AgainstNull(dataTree, nameof(dataTree));

			MyDataTree = dataTree;
			MyDataTreeStackContextMenuFactory = MyDataTree.DataTreeStackContextMenuFactory;
		}

		#region Implementation of IToolUiWidgetManager

		/// <inheritdoc />
		void IToolUiWidgetManager.Initialize(MajorFlexComponentParameters majorFlexComponentParameters, Dictionary<string, EventHandler> sharedEventHandlers, IRecordList recordList)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
			Guard.AgainstNull(sharedEventHandlers, nameof(sharedEventHandlers));
			Guard.AgainstNull(recordList, nameof(recordList));

			_cache = majorFlexComponentParameters.LcmCache;
			_sharedEventHandlers = sharedEventHandlers;
			MyRecordList = recordList;

			RegisterHotLinkMenus();
			RegisterSliceLeftEdgeMenus();
		}
		#endregion

		#region Implementation of IDisposable

		private bool _isDisposed;

		~LexiconEditToolDataTreeStackLexSenseManager()
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
			}

			_isDisposed = true;
		}
		#endregion Implementation of IDisposable

		#region hotlinks

		private void RegisterHotLinkMenus()
		{
			// mnuDataTree-ExtendedNote-Hotlinks (LexSense: mnuDataTree_ExtendedNote_Hotlinks)
			MyDataTreeStackContextMenuFactory.HotlinksMenuFactory.RegisterHotlinksMenuCreatorMethod(mnuDataTree_Sense_Hotlinks, Create_mnuDataTree_Sense_Hotlinks); // Only in LexSense.
		}

		private List<Tuple<ToolStripMenuItem, EventHandler>> Create_mnuDataTree_Sense_Hotlinks(Slice slice, string hotlinksMenuId)
		{
			if (hotlinksMenuId != mnuDataTree_Sense_Hotlinks)
			{
				throw new ArgumentException($"Expected argument value of '{mnuDataTree_Sense_Hotlinks}', but got '{hotlinksMenuId}' instead.");
			}
			var hotlinksMenuItemList = new List<Tuple<ToolStripMenuItem, EventHandler>>(2);

			// <item command="CmdDataTree-Insert-Example"/>
			ToolStripMenuItemFactory.CreateHotLinkToolStripMenuItem(hotlinksMenuItemList, Insert_Example_Clicked, LexiconResources.Insert_Example);

			// <item command="CmdDataTree-Insert-SenseBelow"/>
			ToolStripMenuItemFactory.CreateHotLinkToolStripMenuItem(hotlinksMenuItemList, Insert_SenseBelow_Clicked, LexiconResources.Insert_Sense, LexiconResources.InsertSenseToolTip);

			return hotlinksMenuItemList;
		}


		private void Insert_Example_Clicked(object sender, EventArgs e)
		{
#if RANDYTODO
			// TODO: "CmdDataTree-Insert-Example"
#endif
		}

		private void Insert_SenseBelow_Clicked(object sender, EventArgs e)
		{
			// Get slice and see what sense is currently selected, so we can add the new sense after (read: 'below") it.
			var currentSlice = MyDataTree.CurrentSlice;
			ILexSense currentSense;
			while (true)
			{
				var currentObject = currentSlice.MyCmObject;
				if (currentObject is ILexSense)
				{
					currentSense = (ILexSense)currentObject;
					break;
				}
				currentSlice = currentSlice.ParentSlice;
			}
			if (currentSense.Owner is ILexSense)
			{
				var owningSense = (ILexSense)currentSense.Owner;
				LexSenseUi.CreateNewLexSense(_cache, owningSense, owningSense.SensesOS.IndexOf(currentSense) + 1);
			}
			else
			{
				var owningEntry = (ILexEntry)MyRecordList.CurrentObject;
				LexSenseUi.CreateNewLexSense(_cache, owningEntry, owningEntry.SensesOS.IndexOf(currentSense) + 1);
			}
		}

		#endregion hotlinks

		#region slice context menus

		private void RegisterSliceLeftEdgeMenus()
		{
			MyDataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(LexiconEditToolConstants.mnuDataTree_Sense, Create_mnuDataTree_Sense);
		}

		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_Sense(Slice slice, string contextMenuId)
		{
			// Start: <menu id="mnuDataTree-Sense">
			var contextMenuStrip = new ContextMenuStrip
			{
				Name = LexiconEditToolConstants.mnuDataTree_Sense
			};
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(21);

			/*
			<command id="CmdDataTree-Insert-Example" label="Insert _Example" message="DataTreeInsert">
				<parameters field="Examples" className="LexExampleSentence" />
			</command>
			<item command="CmdDataTree-Insert-Example"/>

			<command id="CmdFindExampleSentence" label="Find example sentence..." message="LaunchGuiControl">
				<parameters field="Example" ownerClass="LexExampleSentence" guicontrol="findExampleSentences" />
			</command>
			<item command="CmdFindExampleSentence"/>
			*/
			// TODO: Add above menus.

			// <item command="CmdDataTree-Insert-ExtNote"/>
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers[LexiconEditToolConstants.CmdInsertExtNote], LexiconResources.Insert_Extended_Note);

			// <item command="CmdDataTree-Insert-SenseBelow"/>
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers[LexiconEditToolConstants.CmdInsertSense], LexiconResources.Insert_Sense, LexiconResources.InsertSenseToolTip);

			// <item command="CmdDataTree-Insert-SubSense"/>
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers[LexiconEditToolConstants.CmdInsertSubsense], LexiconResources.Insert_Subsense, LexiconResources.Insert_Subsense_Tooltip);

			// <item command="CmdInsertPicture" label="Insert _Picture" defaultVisible="false"/>
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers[LexiconEditToolConstants.CmdInsertPicture], LexiconResources.Insert_Picture, LexiconResources.Insert_Picture_Tooltip);

			// TODO: Add below menus.
			/*
			<item label="-" translate="do not translate"/>
			<item command="CmdSenseJumpToConcordance"/>
			<item label="-" translate="do not translate"/>
			<item command="CmdDataTree-MoveUp-Sense"/>
			<item command="CmdDataTree-MoveDown-Sense"/>
			<item command="CmdDataTree-MakeSub-Sense"/>
			<item command="CmdDataTree-Promote-Sense"/>
			<item label="-" translate="do not translate"/>
			<item command="CmdDataTree-Merge-Sense"/>
			<item command="CmdDataTree-Split-Sense"/>
			*/

			//<item command="CmdDataTree-Delete-Sense"/>
			var toolStripMenuItem = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Delete_Sense_Clicked, LexiconResources.DeleteSenseAndSubsenses, image: LanguageExplorerResources.Delete);
			toolStripMenuItem.ImageTransparentColor = Color.Magenta;
			// End: <menu id="mnuDataTree-Sense">

			return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
		}

		private void Delete_Sense_Clicked(object sender, EventArgs e)
		{
			MyDataTree.CurrentSlice.HandleDeleteCommand();
		}

		#endregion slice context menus
	}
}