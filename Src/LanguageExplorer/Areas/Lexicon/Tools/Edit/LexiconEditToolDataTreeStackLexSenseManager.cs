// Copyright (c) 2018 SIL International
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
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
	/// <summary>
	/// Implementation that supports the addition(s) to the DataTree's context menus and hotlinks for a LexSense, and objects it owns, in the Lexicon Edit tool.
	/// </summary>
	internal sealed class LexiconEditToolDataTreeStackLexSenseManager : IToolUiWidgetManager
	{
		private const string mnuDataTree_Sense_Hotlinks = "mnuDataTree-Sense-Hotlinks";
		private const string mnuDataTree_ExtendedNote_Hotlinks = "mnuDataTree-ExtendedNote-Hotlinks";
		private ISharedEventHandlers _sharedEventHandlers;
		private IRecordList MyRecordList { get; set; }
		private DataTreeStackContextMenuFactory MyDataTreeStackContextMenuFactory { get; }
		private DataTree MyDataTree { get; }
		private LcmCache _cache;
		private StatusBar _statusBar;
		private IFwMainWnd _mainWnd;
		private FlexComponentParameters _flexComponentParameters;

		internal LexiconEditToolDataTreeStackLexSenseManager(DataTree dataTree)
		{
			Guard.AgainstNull(dataTree, nameof(dataTree));

			MyDataTree = dataTree;
			MyDataTreeStackContextMenuFactory = MyDataTree.DataTreeStackContextMenuFactory;
		}

		#region Implementation of IToolUiWidgetManager

		/// <inheritdoc />
		void IToolUiWidgetManager.Initialize(MajorFlexComponentParameters majorFlexComponentParameters, IRecordList recordList)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
			Guard.AgainstNull(recordList, nameof(recordList));

			_cache = majorFlexComponentParameters.LcmCache;
			_statusBar = majorFlexComponentParameters.StatusBar;
			_sharedEventHandlers = majorFlexComponentParameters.SharedEventHandlers;
			MyRecordList = recordList;
			_mainWnd = majorFlexComponentParameters.MainWindow;
			_flexComponentParameters = majorFlexComponentParameters.FlexComponentParameters;

			RegisterHotLinkMenus();
			RegisterSliceLeftEdgeMenus();
		}

		/// <inheritdoc />
		void IToolUiWidgetManager.UnwireSharedEventHandlers()
		{
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
			// mnuDataTree-ExtendedNote-Hotlinks
			MyDataTreeStackContextMenuFactory.HotlinksMenuFactory.RegisterHotlinksMenuCreatorMethod(mnuDataTree_ExtendedNote_Hotlinks, Create_mnuDataTree_ExtendedNote_Hotlinks);

			// mnuDataTree-Sense-Hotlinks
			MyDataTreeStackContextMenuFactory.HotlinksMenuFactory.RegisterHotlinksMenuCreatorMethod(mnuDataTree_Sense_Hotlinks, Create_mnuDataTree_Sense_Hotlinks);
		}

		private List<Tuple<ToolStripMenuItem, EventHandler>> Create_mnuDataTree_ExtendedNote_Hotlinks(Slice slice, string hotlinksMenuId)
		{
			Require.That(hotlinksMenuId == mnuDataTree_ExtendedNote_Hotlinks, $"Expected argument value of '{mnuDataTree_ExtendedNote_Hotlinks}', but got '{hotlinksMenuId}' instead.");

			var hotlinksMenuItemList = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);

			// <command id="CmdDataTree-Insert-ExtNote" label="Insert Extended Note" message="DataTreeInsert">
			ToolStripMenuItemFactory.CreateHotLinkToolStripMenuItem(hotlinksMenuItemList, _sharedEventHandlers.Get(LexiconEditToolConstants.CmdInsertExtNote), LexiconResources.Insert_Extended_Note);

			return hotlinksMenuItemList;
		}

		private List<Tuple<ToolStripMenuItem, EventHandler>> Create_mnuDataTree_Sense_Hotlinks(Slice slice, string hotlinksMenuId)
		{
			Require.That(hotlinksMenuId == mnuDataTree_Sense_Hotlinks, $"Expected argument value of '{mnuDataTree_Sense_Hotlinks}', but got '{hotlinksMenuId}' instead.");

			var hotlinksMenuItemList = new List<Tuple<ToolStripMenuItem, EventHandler>>(2);

			//<command id="CmdDataTree-Insert-Example" label="Insert _Example" message="DataTreeInsert">
			ToolStripMenuItemFactory.CreateHotLinkToolStripMenuItem(hotlinksMenuItemList, Insert_Example_Clicked, LexiconResources.Insert_Example);

			// <item command="CmdDataTree-Insert-SenseBelow"/>
			ToolStripMenuItemFactory.CreateHotLinkToolStripMenuItem(hotlinksMenuItemList, Insert_SenseBelow_Clicked, LexiconResources.Insert_Sense, LexiconResources.InsertSenseToolTip);

			return hotlinksMenuItemList;
		}

		private void Insert_Example_Clicked(object sender, EventArgs e)
		{
			AreaServices.UndoExtension(LexiconResources.Insert_Example, _cache.ActionHandlerAccessor, () =>
			{
				var sense = (ILexSense)MyDataTree.CurrentSlice.MyCmObject;
				sense.ExamplesOS.Add(_cache.ServiceLocator.GetInstance<ILexExampleSentenceFactory>().Create());
			});
		}

		private void Insert_Translation_Clicked(object sender, EventArgs e)
		{
			MyDataTree.CurrentSlice.HandleInsertCommand("Translations", CmTranslationTags.kClassName, LexExampleSentenceTags.kClassName);
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
			// mnuDataTree-Sense
			MyDataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(LexiconEditToolConstants.mnuDataTree_Sense, Create_mnuDataTree_Sense);

			// mnuDataTree-Example
			MyDataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(LexiconEditToolConstants.mnuDataTree_Example, Create_mnuDataTree_Example);

			// <menu id="mnuDataTree-ExtendedNotes">
			MyDataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(LexiconEditToolConstants.mnuDataTree_ExtendedNotes, Create_mnuDataTree_ExtendedNotes);

			// <menu id="mnuDataTree-ExtendedNote">
			MyDataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(LexiconEditToolConstants.mnuDataTree_ExtendedNote, Create_mnuDataTree_ExtendedNote);

			// <menu id="mnuDataTree-ExtendedNote-Examples">
			MyDataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(LexiconEditToolConstants.mnuDataTree_ExtendedNote_Examples, Create_mnuDataTree_ExtendedNote_Examples);

			// <menu id="mnuDataTree-Picture">
			MyDataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(LexiconEditToolConstants.mnuDataTree_Picture, Create_mnuDataTree_Picture);

			// NB: I don't see "SubSenses" in shipping code.
			// <menu id="mnuDataTree-Subsenses">
			MyDataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(LexiconEditToolConstants.mnuDataTree_Subsenses, Create_mnuDataTree_Subsenses);
		}

		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_ExtendedNotes(Slice slice, string contextMenuId)
		{
			Require.That(contextMenuId == LexiconEditToolConstants.mnuDataTree_ExtendedNotes, $"Expected argument value of '{LexiconEditToolConstants.mnuDataTree_ExtendedNotes}', but got '{contextMenuId}' instead.");

			// Start: <menu id="mnuDataTree-ExtendedNotes">

			var contextMenuStrip = new ContextMenuStrip
			{
				Name = LexiconEditToolConstants.mnuDataTree_ExtendedNotes
			};
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);

			// <command id="CmdDataTree-Insert-ExtNote" label="Insert Extended Note" message="DataTreeInsert">
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Insert_ExtNote_Clicked, LexiconResources.Insert_Extended_Note);

			// End: <menu id="mnuDataTree-ExtendedNotes">

			return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
		}

		private void Insert_ExtNote_Clicked(object sender, EventArgs e)
		{
			MyDataTree.CurrentSlice.HandleInsertCommand("ExtendedNote", LexExtendedNoteTags.kClassName);
		}

		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_ExtendedNote(Slice slice, string contextMenuId)
		{
			Require.That(contextMenuId == LexiconEditToolConstants.mnuDataTree_ExtendedNote, $"Expected argument value of '{LexiconEditToolConstants.mnuDataTree_ExtendedNote}', but got '{contextMenuId}' instead.");

			// Start: <menu id="mnuDataTree-ExtendedNote">

			var contextMenuStrip = new ContextMenuStrip
			{
				Name = LexiconEditToolConstants.mnuDataTree_ExtendedNote
			};
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(6);

			// <command id="CmdDataTree-Delete-ExtNote" label="Delete Extended Note" message="DataTreeDelete" icon="Delete">
			AreaServices.CreateDeleteMenuItem(menuItems, contextMenuStrip, slice, LexiconResources.Delete_Extended_Note, _sharedEventHandlers.Get(AreaServices.DataTreeDelete));

			// <item label="-" translate="do not translate"/>
			ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);

			using (var imageHolder = new LanguageExplorer.DictionaryConfiguration.ImageHolder())
			{
				// <command id="CmdDataTree-MoveUp-ExtNote" label="Move Extended Note _Up" message="MoveUpObjectInSequence" icon="MoveUp">
				var menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(LexiconAreaConstants.MoveUpObjectInOwningSequence), LexiconResources.Move_Extended_Note_Up, image: imageHolder.smallCommandImages.Images[AreaServices.MoveUp]);
				bool visible;
				menu.Enabled = AreaServices.CanMoveUpObjectInOwningSequence(MyDataTree, _cache, out visible);

				// <command id="CmdDataTree-MoveDown-ExtNote" label="Move Extended Note _Down" message="MoveDownObjectInSequence" icon="MoveDown">
				menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(LexiconAreaConstants.MoveDownObjectInOwningSequence), LexiconResources.Move_Extended_Note_Down, image: imageHolder.smallCommandImages.Images[AreaServices.MoveDown]);
				menu.Enabled = AreaServices.CanMoveDownObjectInOwningSequence(MyDataTree, _cache, out visible);
			}

			// <item label="-" translate="do not translate"/>
			ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);

			// <command id="CmdDataTree-Insert-ExampleInNote" label="Insert Example in Note" message="DataTreeInsert">
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Insert_ExampleInNote_Clicked, LexiconResources.Insert_Example_in_Note);

			// End: <menu id="mnuDataTree-ExtendedNote">

			return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
		}

		private void Insert_ExampleInNote_Clicked(object sender, EventArgs e)
		{
			MyDataTree.CurrentSlice.HandleInsertCommand("Examples", LexExampleSentenceTags.kClassName, LexExtendedNoteTags.kClassName);
		}

		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_ExtendedNote_Examples(Slice slice, string contextMenuId)
		{
			Require.That(contextMenuId == LexiconEditToolConstants.mnuDataTree_ExtendedNote_Examples, $"Expected argument value of '{LexiconEditToolConstants.mnuDataTree_ExtendedNote_Examples}', but got '{contextMenuId}' instead.");

			// Start: <menu id="mnuDataTree-ExtendedNote-Examples">

			var contextMenuStrip = new ContextMenuStrip
			{
				Name = LexiconEditToolConstants.mnuDataTree_ExtendedNote_Examples
			};
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(5);

			// <command id="CmdDataTree-Insert-ExampleInNote" label="Insert Example in Note" message="DataTreeInsert">
			var menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Insert_ExampleInNote_Clicked, LexiconResources.Insert_Example_in_Note);

			// <command id="CmdDataTree-Delete-ExampleInNote" label="Delete Example from Note" message="DataTreeDelete" icon="Delete">
			AreaServices.CreateDeleteMenuItem(menuItems, contextMenuStrip, slice, LexiconResources.Delete_Example_from_Note, _sharedEventHandlers.Get(AreaServices.DataTreeDelete));

			// <item label="-" translate="do not translate"/>
			ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);

			using (var imageHolder = new LanguageExplorer.DictionaryConfiguration.ImageHolder())
			{
				// <command id="CmdDataTree-MoveUp-ExampleInNote" label="Move Example _Up" message="MoveUpObjectInSequence" icon="MoveUp">
				menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(LexiconAreaConstants.MoveUpObjectInOwningSequence), LexiconResources.Move_Example_Up, image: imageHolder.smallCommandImages.Images[AreaServices.MoveUp]);
				bool visible;
				menu.Enabled = AreaServices.CanMoveUpObjectInOwningSequence(MyDataTree, _cache, out visible);

				// <command id="CmdDataTree-MoveDown-ExampleInNote" label="Move Example _Down" message="MoveDownObjectInSequence" icon="MoveDown">
				menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(LexiconAreaConstants.MoveDownObjectInOwningSequence), LexiconResources.Move_Example_Down, image: imageHolder.smallCommandImages.Images[AreaServices.MoveDown]);
				menu.Enabled = AreaServices.CanMoveDownObjectInOwningSequence(MyDataTree, _cache, out visible);
			}

			// End: <menu id="mnuDataTree-ExtendedNote-Examples">

			return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
		}

		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_Picture(Slice slice, string contextMenuId)
		{
			Require.That(contextMenuId == LexiconEditToolConstants.mnuDataTree_Picture, $"Expected argument value of '{LexiconEditToolConstants.mnuDataTree_Picture}', but got '{contextMenuId}' instead.");

			// Start: <menu id="mnuDataTree-Picture">

			var contextMenuStrip = new ContextMenuStrip
			{
				Name = LexiconEditToolConstants.mnuDataTree_Picture
			};
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(6);

			// <command id="CmdDataTree-Properties-Picture" label="Picture Properties" message="PictureProperties">
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Properties_Picture_Clicked, LexiconResources.Picture_Properties);

			// <item label="-" translate="do not translate"/>
			ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);

			using (var imageHolder = new LanguageExplorer.DictionaryConfiguration.ImageHolder())
			{
				// <command id="CmdDataTree-MoveUp-Picture" label="Move Picture _Up" message="MoveUpObjectInSequence" icon="MoveUp">
				var menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(LexiconAreaConstants.MoveUpObjectInOwningSequence), LexiconResources.Move_Picture_Up, image: imageHolder.smallCommandImages.Images[AreaServices.MoveUp]);
				bool visible;
				menu.Enabled = AreaServices.CanMoveUpObjectInOwningSequence(MyDataTree, _cache, out visible);

				// <command id="CmdDataTree-MoveDown-Picture" label="Move Picture _Down" message="MoveDownObjectInSequence" icon="MoveDown">
				menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(LexiconAreaConstants.MoveDownObjectInOwningSequence), LexiconResources.Move_Picture_Down, image: imageHolder.smallCommandImages.Images[AreaServices.MoveDown]);
				menu.Enabled = AreaServices.CanMoveDownObjectInOwningSequence(MyDataTree, _cache, out visible);
			}

			// <item label="-" translate="do not translate"/>
			ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);

			// <command id="CmdDataTree-Delete-Picture" label="Delete Picture" message="DataTreeDelete" icon="Delete">
			AreaServices.CreateDeleteMenuItem(menuItems, contextMenuStrip, slice, LexiconResources.Delete_Picture, _sharedEventHandlers.Get(AreaServices.DataTreeDelete));

			// End: <menu id="mnuDataTree-Picture">

			return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
		}

		private void Properties_Picture_Clicked(object sender, EventArgs e)
		{
			var slice = MyDataTree.CurrentSlice;
			var pictureSlices = new List<PictureSlice>();

			// Create an array of potential slices to call the showProperties method on.  If we're being called from a PictureSlice,
			// there's no need to go through the whole list, so we can be a little more intelligent
			if (slice is PictureSlice)
			{
				pictureSlices.Add(slice as PictureSlice);
			}
			else
			{
				foreach (var otherSlice in MyDataTree.Slices)
				{
					if (otherSlice is PictureSlice && !ReferenceEquals(slice, otherSlice))
					{
						pictureSlices.Add(otherSlice as PictureSlice);
					}
				}
			}

			foreach (var pictureSlice in pictureSlices)
			{
				// Make sure the target slice refers to the same object that we do
				if (ReferenceEquals(pictureSlice.MyCmObject, slice.MyCmObject))
				{
					pictureSlice.showProperties();
					break;
				}
			}
		}

		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_Subsenses(Slice slice, string contextMenuId)
		{
			Require.That(contextMenuId == LexiconEditToolConstants.mnuDataTree_Subsenses, $"Expected argument value of '{LexiconEditToolConstants.mnuDataTree_Subsenses}', but got '{contextMenuId}' instead.");

			// Start: <menu id="mnuDataTree-Subsenses">

			var contextMenuStrip = new ContextMenuStrip
			{
				Name = LexiconEditToolConstants.mnuDataTree_Subsenses
			};
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);

			// <item command="CmdDataTree-Insert-SubSense"/>
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(LexiconEditToolConstants.CmdInsertSubsense), LexiconResources.Insert_Subsense, LexiconResources.Insert_Subsense_Tooltip);

			// End: <menu id="mnuDataTree-Subsenses">

			return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
		}

		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_Example(Slice slice, string contextMenuId)
		{
			Require.That(contextMenuId == LexiconEditToolConstants.mnuDataTree_Example, $"Expected argument value of '{LexiconEditToolConstants.mnuDataTree_Example}', but got '{contextMenuId}' instead.");

			// Start: <menu id="mnuDataTree-Example">

			var contextMenuStrip = new ContextMenuStrip
			{
				Name = LexiconEditToolConstants.mnuDataTree_Example
			};
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(7);

			// <command id="CmdDataTree-Insert-Translation" label="Insert Translation" message="DataTreeInsert">
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Insert_Translation_Clicked, LexiconResources.Insert_Translation);

			// <command id="CmdDataTree-Delete-Example" label="Delete Example" message="DataTreeDelete" icon="Delete">
			AreaServices.CreateDeleteMenuItem(menuItems, contextMenuStrip, slice, LexiconResources.Delete_Example, _sharedEventHandlers.Get(AreaServices.DataTreeDelete));

			// <item label="-" translate="do not translate"/>
			ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);

			using (var imageHolder = new LanguageExplorer.DictionaryConfiguration.ImageHolder())
			{
				// <command id="CmdDataTree-MoveUp-Example" label="Move Example _Up" message="MoveUpObjectInSequence" icon="MoveUp">
				var menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(LexiconAreaConstants.MoveUpObjectInOwningSequence), LexiconResources.Move_Example_Up, image: imageHolder.smallCommandImages.Images[AreaServices.MoveUp]);
				bool visible;
				menu.Enabled = AreaServices.CanMoveUpObjectInOwningSequence(MyDataTree, _cache, out visible);

				// <command id="CmdDataTree-MoveDown-Example" label="Move Example _Down" message="MoveDownObjectInSequence" icon="MoveDown">
				menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(LexiconAreaConstants.MoveDownObjectInOwningSequence), LexiconResources.Move_Example_Down, image: imageHolder.smallCommandImages.Images[AreaServices.MoveDown]);
				menu.Enabled = AreaServices.CanMoveDownObjectInOwningSequence(MyDataTree, _cache, out visible);
			}

			// <item label="-" translate="do not translate"/>
			ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);

			// <command id="CmdFindExampleSentence" label="Find example sentence..." message="LaunchGuiControl">
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, FindExampleSentence_Clicked, LexiconResources.Find_example_sentence);

			// End: <menu id="mnuDataTree-Example">

			return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
		}

		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_Sense(Slice slice, string contextMenuId)
		{
			Require.That(contextMenuId == LexiconEditToolConstants.mnuDataTree_Sense, $"Expected argument value of '{LexiconEditToolConstants.mnuDataTree_Sense}', but got '{contextMenuId}' instead.");

			// Start: <menu id="mnuDataTree-Sense">
			var contextMenuStrip = new ContextMenuStrip
			{
				Name = LexiconEditToolConstants.mnuDataTree_Sense
			};
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(21);

			//<command id="CmdDataTree-Insert-Example" label="Insert _Example" message="DataTreeInsert">
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Insert_Example_Clicked, LexiconResources.Insert_Example);

			// <command id="CmdFindExampleSentence" label="Find example sentence..." message="LaunchGuiControl">
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, FindExampleSentence_Clicked, LexiconResources.Find_example_sentence);

			// <item command="CmdDataTree-Insert-ExtNote"/>
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(LexiconEditToolConstants.CmdInsertExtNote), LexiconResources.Insert_Extended_Note);

			// <item command="CmdDataTree-Insert-SenseBelow"/>
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(LexiconEditToolConstants.CmdInsertSense), LexiconResources.Insert_Sense, LexiconResources.InsertSenseToolTip);

			// <item command="CmdDataTree-Insert-SubSense"/>
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(LexiconEditToolConstants.CmdInsertSubsense), LexiconResources.Insert_Subsense, LexiconResources.Insert_Subsense_Tooltip);

			// <item command="CmdInsertPicture" label="Insert _Picture" defaultVisible="false"/>
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(LexiconEditToolConstants.CmdInsertPicture), LexiconResources.Insert_Picture, LexiconResources.Insert_Picture_Tooltip);

			// <item label="-" translate="do not translate"/>
			ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);

			// <command id="CmdSenseJumpToConcordance" label="Show Sense in Concordance" message="JumpToTool">
			var menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(AreaServices.JumpToTool), AreaResources.Show_Sense_in_Concordance);
			menu.Tag = new List<object> { _flexComponentParameters.Publisher, AreaServices.ConcordanceMachineName, MyRecordList.CurrentObject.Guid };

			// <item label="-" translate="do not translate"/>
			ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);

			using (var imageHolder = new LanguageExplorer.DictionaryConfiguration.ImageHolder())
			{
				// <command id="CmdDataTree-MoveUp-Sense" label="Move Sense Up" message="MoveUpObjectInSequence" icon="MoveUp">
				menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(LexiconAreaConstants.MoveUpObjectInOwningSequence), LexiconResources.Move_Sense_Up, image: imageHolder.smallCommandImages.Images[AreaServices.MoveUp]);
				bool visible;
				menu.Enabled = AreaServices.CanMoveUpObjectInOwningSequence(MyDataTree, _cache, out visible);

				// <command id="CmdDataTree-MoveDown-Sense" label="Move Sense Down" message="MoveDownObjectInSequence" icon="MoveDown">
				menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(LexiconAreaConstants.MoveDownObjectInOwningSequence), LexiconResources.Move_Sense_Down, image: imageHolder.smallCommandImages.Images[AreaServices.MoveDown]);
				menu.Enabled = AreaServices.CanMoveDownObjectInOwningSequence(MyDataTree, _cache, out visible);

				// <command id="CmdDataTree-MakeSub-Sense" label="Demote" message="DemoteSense" icon="MoveRight">
				menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Demote_Sense_Clicked, AreaResources.Demote, image: imageHolder.smallCommandImages.Images[AreaServices.MoveRight]);
				menu.Enabled = CanDemoteSense(slice);

				// <command id="CmdDataTree-Promote-Sense" label="Promote" message="PromoteSense" icon="MoveLeft">
				menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Promote_Sense_Clicked, AreaResources.Promote, image: imageHolder.smallCommandImages.Images[AreaServices.MoveLeft]);
				menu.Enabled = CanPromoteSense(slice);
			}

			// <item label="-" translate="do not translate"/>
			ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);

			// <command id="CmdDataTree-Merge-Sense" label="Merge Sense into..." message="DataTreeMerge">
			menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(LexiconAreaConstants.DataTreeMerge), LexiconResources.Merge_Sense_into);
			menu.Enabled = slice.CanMergeNow;

			// <command id="CmdDataTree-Split-Sense" label="Move Sense to a New Entry" message="DataTreeSplit">
			menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(LexiconAreaConstants.DataTreeSplit), LexiconResources.Move_Sense_to_a_New_Entry);
			menu.Enabled = slice.CanSplitNow;

			// <command id="CmdDataTree-Delete-Sense" label="Delete this Sense and any Subsenses" message="DataTreeDeleteSense" icon="Delete">
			AreaServices.CreateDeleteMenuItem(menuItems, contextMenuStrip, slice, LexiconResources.DeleteSenseAndSubsenses, _sharedEventHandlers.Get(AreaServices.DataTreeDelete));

			// End: <menu id="mnuDataTree-Sense">

			return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
		}

		private bool CanDemoteSense(Slice currentSlice)
		{
			return currentSlice.MyCmObject is ILexSense && _cache.DomainDataByFlid.get_VecSize(currentSlice.MyCmObject.Owner.Hvo, currentSlice.MyCmObject.OwningFlid) > 1;
		}

		private void Demote_Sense_Clicked(object sender, EventArgs e)
		{
			UndoableUnitOfWorkHelper.Do(LanguageExplorerResources.ksUndoDemote, LanguageExplorerResources.ksRedoDemote, _cache.ActionHandlerAccessor, () =>
			{
				var sense = MyDataTree.CurrentSlice.MyCmObject;
				var hvoOwner = sense.Owner.Hvo;
				var owningFlid = sense.OwningFlid;
				var ihvo = _cache.DomainDataByFlid.GetObjIndex(hvoOwner, owningFlid, sense.Hvo);
				var hvoNewOwner = _cache.DomainDataByFlid.get_VecItem(hvoOwner, owningFlid, (ihvo == 0) ? 1 : ihvo - 1);
				_cache.DomainDataByFlid.MoveOwnSeq(hvoOwner, owningFlid, ihvo, ihvo, hvoNewOwner, LexSenseTags.kflidSenses, _cache.DomainDataByFlid.get_VecSize(hvoNewOwner, LexSenseTags.kflidSenses));
			});
		}

		private static bool CanPromoteSense(Slice currentSlice)
		{
			var moveeSense = currentSlice.MyCmObject as ILexSense;
			var oldOwningSense = currentSlice.MyCmObject.Owner as ILexSense;
			// Can't promote top-level sense or something that isn't a sense.
			return moveeSense != null && oldOwningSense != null;
		}

		private void Promote_Sense_Clicked(object sender, EventArgs e)
		{
			AreaServices.UndoExtension(AreaResources.Promote, _cache.ActionHandlerAccessor, () =>
			{
				var slice = MyDataTree.CurrentSlice;
				var oldOwner = slice.MyCmObject.Owner;
				var index = oldOwner.IndexInOwner;
				var newOwner = oldOwner.Owner;
				if (newOwner is ILexEntry)
				{
					var newOwningEntry = (ILexEntry)newOwner;
					newOwningEntry.SensesOS.Insert(index + 1, slice.MyCmObject as ILexSense);
				}
				else
				{
					var newOwningSense = (ILexSense)newOwner;
					newOwningSense.SensesOS.Insert(index + 1, slice.MyCmObject as ILexSense);
				}
			});
		}

		private void FindExampleSentence_Clicked(object sender, EventArgs e)
		{
			using (var findExampleSentencesDlg = new FindExampleSentenceDlg(_statusBar, MyDataTree.CurrentSlice.MyCmObject, MyRecordList))
			{
				findExampleSentencesDlg.InitializeFlexComponent(_flexComponentParameters);
				findExampleSentencesDlg.ShowDialog((Form)_mainWnd);
			}
		}

		#endregion slice context menus
	}
}