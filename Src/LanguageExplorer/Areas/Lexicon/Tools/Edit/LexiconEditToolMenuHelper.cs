// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Areas.Lexicon.DictionaryConfiguration;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.DetailControls;
using LanguageExplorer.Controls.LexText;
using LanguageExplorer.LcmUi;
using SIL.Code;
using SIL.FieldWorks.Common.Controls.FileDialog;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.FieldWorks.Resources;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
	/// <summary>
	/// This class handles all interaction for the LexiconEditTool for its menus, toolbars, plus all context menus that are used in Slices and PaneBars.
	/// </summary>
	internal sealed class LexiconEditToolMenuHelper : IFlexComponent, IDisposable
	{
		private LexiconAreaMenuHelper _lexiconAreaMenuHelper;
		internal const string Show_DictionaryPubPreview = "Show_DictionaryPubPreview";
		internal const string panelMenuId = "left";
		private const string mnuDataTree_Sense_Hotlinks = "mnuDataTree-Sense-Hotlinks";
		private const string mnuDataTree_Sense = "mnuDataTree-Sense";
		private const string mnuDataTree_Etymology = "mnuDataTree-Etymology";
		private const string mnuDataTree_Etymology_Hotlinks = "mnuDataTree-Etymology-Hotlinks";
		private const string mnuDataTree_AlternateForms = "mnuDataTree-AlternateForms";
		private const string mnuDataTree_AlternateForms_Hotlinks = "mnuDataTree-AlternateForms-Hotlinks";
		private const string mnuDataTree_Pronunciation = "mnuDataTree-Pronunciation";
		private string _extendedPropertyName;
		private MajorFlexComponentParameters _majorFlexComponentParameters;
		private ToolStripMenuItem _editMenu;
		private List<Tuple<ToolStripMenuItem, EventHandler>> _newEditMenusAndHandlers = new List<Tuple<ToolStripMenuItem, EventHandler>>();
		private ToolStripMenuItem _insertMenu;
		private List<Tuple<ToolStripMenuItem, EventHandler>> _newInsertMenusAndHandlers = new List<Tuple<ToolStripMenuItem, EventHandler>>();
		private ToolStripMenuItem _viewMenu;
		private List<Tuple<ToolStripMenuItem, EventHandler>> _newViewMenusAndHandlers = new List<Tuple<ToolStripMenuItem, EventHandler>>();
		private ToolStripMenuItem _show_DictionaryPubPreviewMenu;
		private ToolStripMenuItem _show_DictionaryPubPreviewContextMenu;
		private ToolStripMenuItem _showHiddenFieldsMenu;
		private ToolStripButton _insertEntryToolStripButton;
		private ToolStripButton _insertGoToEntryToolStripButton;
		private ToolStripMenuItem _toolsConfigureMenu;
		private List<Tuple<ToolStripMenuItem, EventHandler>> _newToolsConfigurationMenusAndHandlers = new List<Tuple<ToolStripMenuItem, EventHandler>>();
		private ToolStripMenuItem _toolsMenu;
		private List<Tuple<ToolStripMenuItem, EventHandler>> _newToolsMenusAndHandlers = new List<Tuple<ToolStripMenuItem, EventHandler>>();
		private ToolStripSeparator _toolMenuToolStripSeparator;
		private readonly List<ToolStripItem> _senseMenuItems = new List<ToolStripItem>();
		private DataTree MyDataTree { get; set; }
		private RecordBrowseView RecordBrowseView { get; }
		private IRecordList MyRecordList { get; set; }
		internal MultiPane InnerMultiPane { get; set; }
		internal SliceContextMenuFactory SliceContextMenuFactory { get; set; }

		internal LexiconEditToolMenuHelper(MajorFlexComponentParameters majorFlexComponentParameters, DataTree dataTree, RecordBrowseView recordBrowseView, IRecordList recordList, string extendedPropertyName)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
			Guard.AgainstNull(dataTree, nameof(dataTree));
			Guard.AgainstNull(recordList, nameof(recordList));
			Guard.AgainstNullOrEmptyString(extendedPropertyName, nameof(extendedPropertyName));

			_majorFlexComponentParameters = majorFlexComponentParameters;
			MyDataTree = dataTree;
			RecordBrowseView = recordBrowseView;
			MyDataTree.CurrentSliceChanged += MyDataTree_CurrentSliceChanged;
			MyRecordList = recordList;
			SliceContextMenuFactory = MyDataTree.SliceContextMenuFactory;
			_lexiconAreaMenuHelper = new LexiconAreaMenuHelper(_majorFlexComponentParameters, MyRecordList);
			_extendedPropertyName = extendedPropertyName;

			InitializeFlexComponent(_majorFlexComponentParameters.FlexComponentParameters);
		}

		internal void Initialize()
		{
			_lexiconAreaMenuHelper.Initialize();
			_lexiconAreaMenuHelper.MyAreaWideMenuHelper.SetupToolsCustomFieldsMenu();

			AddEditMenuItems();
			AddViewMenuItems();
			AddInsertMenuItems();
			AddToolsMenuItems();

			AddToolbarItems();

			RegisterHotLinkMenus();
			RegisterOrdinaryContextMenus();
			SliceContextMenuFactory.RegisterPanelMenuCreatorMethod(panelMenuId, CreateMainPanelContextMenuStrip);
		}

		private void RegisterHotLinkMenus()
		{
			SliceContextMenuFactory.RegisterHotlinksMenuCreatorMethod(mnuDataTree_Sense_Hotlinks, Create_mnuDataTree_Sense_Hotlinks);
			SliceContextMenuFactory.RegisterHotlinksMenuCreatorMethod(mnuDataTree_Etymology_Hotlinks, Create_mnuDataTree_Etymology_Hotlinks);
			SliceContextMenuFactory.RegisterHotlinksMenuCreatorMethod(mnuDataTree_AlternateForms_Hotlinks, Create_mnuDataTree_AlternateForms_Hotlinks);
		}

		private void RegisterOrdinaryContextMenus()
		{
			SliceContextMenuFactory.RegisterOrdinaryMenuCreatorMethod(mnuDataTree_Sense, Create_mnuDataTree_Sense);
			SliceContextMenuFactory.RegisterOrdinaryMenuCreatorMethod(mnuDataTree_Etymology, Create_mnuDataTree_Etymology);
			SliceContextMenuFactory.RegisterOrdinaryMenuCreatorMethod(mnuDataTree_AlternateForms, Create_mnuDataTree_AlternateForms);
			SliceContextMenuFactory.RegisterOrdinaryMenuCreatorMethod(mnuDataTree_Pronunciation, Create_mnuDataTree_Pronunciation);
		}

		#region Implementation of IPropertyTableProvider

		/// <summary>
		/// Placement in the IPropertyTableProvider interface lets FwApp call IPropertyTable.DoStuff.
		/// </summary>
		public IPropertyTable PropertyTable { get; private set; }

		#endregion

		#region Implementation of IPublisherProvider

		/// <summary>
		/// Get the IPublisher.
		/// </summary>
		public IPublisher Publisher { get; private set; }

		#endregion

		#region Implementation of ISubscriberProvider

		/// <summary>
		/// Get the ISubscriber.
		/// </summary>
		public ISubscriber Subscriber { get; private set; }

		#endregion

		#region Implementation of IFlexComponent

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		public void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			FlexComponentCheckingService.CheckInitializationValues(flexComponentParameters, new FlexComponentParameters(PropertyTable, Publisher, Subscriber));

			PropertyTable = flexComponentParameters.PropertyTable;
			Publisher = flexComponentParameters.Publisher;
			Subscriber = flexComponentParameters.Subscriber;

			Subscriber.Subscribe("ShowHiddenFields", ShowHiddenFields_Handler);
		}

		#endregion

		private void ShowHiddenFields_Handler(object obj)
		{
			_showHiddenFieldsMenu.Checked = (bool)obj;
		}

		private void MyDataTree_CurrentSliceChanged(object sender, CurrentSliceChangedEventArgs e)
		{
			var currentSlice = e.CurrentSlice;
			if (currentSlice.Object == null)
			{
				SenseMenusVisibility(false);
				return;
			}
			var sliceObject = currentSlice.Object;
			if (sliceObject is ILexSense)
			{
				SenseMenusVisibility(true);
				return;
			}
			// "owningSense" will be null, if 'sliceObject' is owned by the entry, but not a sense.
			var owningSense = sliceObject.OwnerOfClass<ILexSense>();
			if (owningSense == null)
			{
				SenseMenusVisibility(false);
				return;
			}

			// We now know that the current slice is a sense or is 'owned' by a sense,
			// so enable the Insert menus that are related to a sense.
			SenseMenusVisibility(true);
		}

		private void SenseMenusVisibility(bool visible)
		{
			// This will make select Insert menus visible.
			foreach (var menuItem in _senseMenuItems)
			{
				menuItem.Visible = visible;
			}
		}

		#region IDisposable
		private bool _isDisposed;

		~LexiconEditToolMenuHelper()
		{
			// The base class finalizer is called automatically.
			Dispose(false);
		}

		/// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
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
				return; // No need to do it more than once.
			}

			if (disposing)
			{
				_senseMenuItems.Clear();
				MyDataTree.CurrentSliceChanged -= MyDataTree_CurrentSliceChanged;
				Subscriber.Unsubscribe("ShowHiddenFields", ShowHiddenFields_Handler);
				_lexiconAreaMenuHelper.Dispose();

				foreach (var menuTuple in _newToolsConfigurationMenusAndHandlers)
				{
					menuTuple.Item1.Click -= menuTuple.Item2;
					_toolsConfigureMenu.DropDownItems.Remove(menuTuple.Item1);
					menuTuple.Item1.Dispose();
				}
				_newToolsConfigurationMenusAndHandlers.Clear();

				foreach (var menuTuple in _newEditMenusAndHandlers)
				{
					menuTuple.Item1.Click -= menuTuple.Item2;
					_editMenu.DropDownItems.Remove(menuTuple.Item1);
					menuTuple.Item1.Dispose();
				}
				_newEditMenusAndHandlers.Clear();

				foreach (var menuTuple in _newInsertMenusAndHandlers)
				{
					menuTuple.Item1.Click -= menuTuple.Item2;
					_insertMenu.DropDownItems.Remove(menuTuple.Item1);
					menuTuple.Item1.Dispose();
				}
				_newInsertMenusAndHandlers.Clear();

				foreach (var menuTuple in _newViewMenusAndHandlers)
				{
					menuTuple.Item1.Click -= menuTuple.Item2;
					_viewMenu.DropDownItems.Remove(menuTuple.Item1);
					menuTuple.Item1.Dispose();
				}
				_newViewMenusAndHandlers.Clear();

				foreach (var menuTuple in _newToolsMenusAndHandlers)
				{
					menuTuple.Item1.Click -= menuTuple.Item2;
					_toolsMenu.DropDownItems.Remove(menuTuple.Item1);
					menuTuple.Item1.Dispose();
				}
				_newToolsMenusAndHandlers.Clear();
				_toolsMenu.DropDownItems.Remove(_toolMenuToolStripSeparator);
				_toolMenuToolStripSeparator.Dispose();

				_insertEntryToolStripButton.Click -= Insert_Entry_Clicked;
				InsertToolbarManager.ResetInsertToolbar(_majorFlexComponentParameters);
				_insertEntryToolStripButton.Dispose();
			}
			_lexiconAreaMenuHelper = null;
			_majorFlexComponentParameters = null;
			_insertMenu = null;
			_insertEntryToolStripButton = null;
			_newEditMenusAndHandlers = null;
			_newInsertMenusAndHandlers = null;
			_newViewMenusAndHandlers = null;
			_newToolsConfigurationMenusAndHandlers = null;
			_toolsMenu = null;
			_toolMenuToolStripSeparator = null;
			_newToolsMenusAndHandlers = null;
			SliceContextMenuFactory = null;
			MyDataTree = null;
			MyRecordList = null;
			InnerMultiPane = null;

			_isDisposed = true;
		}
		#endregion

		private List<Tuple<ToolStripMenuItem, EventHandler>> Create_mnuDataTree_AlternateForms_Hotlinks(Slice slice, string hotlinksMenuId)
		{
			if (hotlinksMenuId != mnuDataTree_AlternateForms_Hotlinks)
			{
				throw new ArgumentException($"Expected argmuent value of '{mnuDataTree_AlternateForms_Hotlinks}', but got '{nameof(hotlinksMenuId)}' instead.");
			}
			var hotlinksMenuItemList = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);

			// <item command="CmdDataTree-Insert-AlternateForm"/>
			ToolStripMenuItemFactory.CreateHotLinkToolStripMenuItem(hotlinksMenuItemList, Insert_Allomorph_Clicked, LexiconResources.Insert_Allomorph);

			return hotlinksMenuItemList;
		}

		private List<Tuple<ToolStripMenuItem, EventHandler>> Create_mnuDataTree_Sense_Hotlinks(Slice slice, string hotlinksMenuId)
		{
			if (hotlinksMenuId != mnuDataTree_Sense_Hotlinks)
			{
				throw new ArgumentException($"Expected argmuent value of '{mnuDataTree_Sense_Hotlinks}', but got '{nameof(hotlinksMenuId)}' instead.");
			}
			var hotlinksMenuItemList = new List<Tuple<ToolStripMenuItem, EventHandler>>(2);

			// <item command="CmdDataTree-Insert-Example"/>
			ToolStripMenuItemFactory.CreateHotLinkToolStripMenuItem(hotlinksMenuItemList, Insert_Example_Clicked, LexiconResources.Insert_Example);

			// <item command="CmdDataTree-Insert-SenseBelow"/>
			ToolStripMenuItemFactory.CreateHotLinkToolStripMenuItem(hotlinksMenuItemList, Insert_SenseBelow_Clicked, LexiconResources.Insert_Sense, LexiconResources.InsertSenseToolTip);

			return hotlinksMenuItemList;
		}

		private List<Tuple<ToolStripMenuItem, EventHandler>> Create_mnuDataTree_Etymology_Hotlinks(Slice slice, string hotlinksMenuId)
		{
			if (hotlinksMenuId != mnuDataTree_Etymology_Hotlinks)
			{
				throw new ArgumentException($"Expected argmuent value of '{mnuDataTree_Etymology_Hotlinks}', but got '{nameof(hotlinksMenuId)}' instead.");
			}
			var hotlinksMenuItemList = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);
			// <item command="CmdDataTree-Insert-Etymology"/>
			ToolStripMenuItemFactory.CreateHotLinkToolStripMenuItem(hotlinksMenuItemList, Insert_Etymology_Clicked, LexiconResources.Insert_Etymology, LexiconResources.Insert_Etymology_Tooltip);

			return hotlinksMenuItemList;
		}

		private Tuple<ContextMenuStrip, CancelEventHandler, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_Pronunciation(Slice slice, string contextMenuId)
		{
			// Start: <menu id="mnuDataTree-Pronunciation">
			var contextMenuStrip = new ContextMenuStrip
			{
				Name = mnuDataTree_AlternateForms
			};
			contextMenuStrip.Opening += MenuDataTree_PronunciationContextMenuStrip_Opening;
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(2);
			// <item command="CmdDataTree-Insert-Pronunciation"/>
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Insert_Pronunciation_Clicked, LexiconResources.Insert_Pronunciation, LexiconResources.Insert_Pronunciation_Tooltip);
			/*
			<item command="CmdInsertMediaFile" label="Insert _Sound or Movie" defaultVisible="false"/>
			<item label="-" translate="do not translate"/>
			<item command="CmdDataTree-MoveUp-Pronunciation"/>
			<item command="CmdDataTree-MoveDown-Pronunciation"/>
			<item label="-" translate="do not translate"/>
			<item command="CmdDataTree-Delete-Pronunciation"/>
			<item label="-" translate="do not translate"/>
			*/
			// End: <menu id="mnuDataTree-Pronunciation>

			return new Tuple<ContextMenuStrip, CancelEventHandler, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, MenuDataTree_PronunciationContextMenuStrip_Opening, menuItems);
		}

		private void MenuDataTree_PronunciationContextMenuStrip_Opening(object sender, CancelEventArgs e)
		{
#if RANDYTODO
// TODO: Enable/disable menu items, based on selected slice in DataTree.
#endif
		}

		private Tuple<ContextMenuStrip, CancelEventHandler, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_AlternateForms(Slice slice, string contextMenuId)
		{
			// Start: <menu id="mnuDataTree-AlternateForms">
			var contextMenuStrip = new ContextMenuStrip
			{
				Name = mnuDataTree_AlternateForms
			};
			contextMenuStrip.Opening += MenuDataTree_AlternateFormsContextMenuStrip_Opening;
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(2);
			// <item command="CmdDataTree-Insert-AlternateForm"/>
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Insert_Allomorph_Clicked, LexiconResources.Insert_Allomorph, LexiconResources.Insert_Allomorph_Tooltip);
			/*
			<item command="CmdDataTree-Insert-AffixProcess"/>
			<command id="CmdDataTree-Insert-AffixProcess" label="Insert Affix Process" message="DataTreeInsert">
				<parameters field="AlternateForms" className="MoAffixProcess"/>
			</command>
			*/
			// End: <menu id="mnuDataTree-AlternateForms">

			return new Tuple<ContextMenuStrip, CancelEventHandler, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, MenuDataTree_AlternateFormsContextMenuStrip_Opening, menuItems);
		}

		private void MenuDataTree_AlternateFormsContextMenuStrip_Opening(object sender, CancelEventArgs e)
		{
#if RANDYTODO
// TODO: Enable/disable menu items, based on selected slice in DataTree.
#endif
		}

		private Tuple<ContextMenuStrip, CancelEventHandler, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_Etymology(Slice slice, string contextMenuId)
		{
			// Start: <menu id="mnuDataTree-Etymology">
			var contextMenuStrip = new ContextMenuStrip
			{
				Name = mnuDataTree_Etymology
			};
			contextMenuStrip.Opening += MenuDataTree_EtymologyContextMenuStrip_Opening;
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(6);

			// <item command="CmdDataTree-Insert-Etymology" label="Insert _Etymology"/>
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Insert_Etymology_Clicked, LexiconResources.Insert_Etymology, LexiconResources.Insert_Etymology_Tooltip);
			/*
			<item label="-" translate="do not translate"/>
			<item command="CmdDataTree-MoveUp-Etymology"/>
			<item command="CmdDataTree-MoveDown-Etymology"/>
			<item label="-" translate="do not translate"/>
			<item command="CmdDataTree-Delete-Etymology"/>
			 */

			// End: <menu id="mnuDataTree-Etymology">

			return new Tuple<ContextMenuStrip, CancelEventHandler, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, MenuDataTree_EtymologyContextMenuStrip_Opening, menuItems);
		}

		private void MenuDataTree_EtymologyContextMenuStrip_Opening(object sender, CancelEventArgs e)
		{
#if RANDYTODO
			// TODO: Enable/disable menu items, based on selected slice in DataTree.
#endif
		}

		private void Insert_Etymology_Clicked(object sender, EventArgs e)
		{
			UndoableUnitOfWorkHelper.Do(LexiconResources.Undo_Insert_Etymology, LexiconResources.Redo_Insert_Etymology, _majorFlexComponentParameters.LcmCache.ServiceLocator.GetInstance<IActionHandler>(), () =>
			{
				((ILexEntry)MyRecordList.CurrentObject).EtymologyOS.Add(_majorFlexComponentParameters.LcmCache.ServiceLocator.GetInstance<ILexEtymologyFactory>().Create());
			});
		}

		private Tuple<ContextMenuStrip, CancelEventHandler, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_Sense(Slice slice, string contextMenuId)
		{
			// Start: <menu id="mnuDataTree-Sense">
			var contextMenuStrip = new ContextMenuStrip
			{
				Name = mnuDataTree_Sense
			};
			contextMenuStrip.Opening += MenuDataTree_SenseContextMenuStrip_Opening;
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
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Insert_ExtendedNote_Clicked, LexiconResources.Insert_Extended_Note);

			// <item command="CmdDataTree-Insert-SenseBelow"/>
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Insert_Sense_Clicked, LexiconResources.Insert_Sense, LexiconResources.InsertSenseToolTip);

			// <item command="CmdDataTree-Insert-SubSense"/>
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Insert_Subsense_Clicked, LexiconResources.Insert_Subsense, LexiconResources.Insert_Subsense_Tooltip);

			// <item command="CmdInsertPicture" label="Insert _Picture" defaultVisible="false"/>
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Insert_Picture_Clicked, LexiconResources.Insert_Picture, LexiconResources.Insert_Picture_Tooltip);

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
			var toolStripMenuItem = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Delete_Sense_Clicked, LexiconResources.DeleteSenseAndSubsenses);
			toolStripMenuItem.Image = LanguageExplorerResources.Delete;
			toolStripMenuItem.ImageTransparentColor = Color.Magenta;
			// End: <menu id="mnuDataTree-Sense">

			return new Tuple<ContextMenuStrip, CancelEventHandler, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, MenuDataTree_SenseContextMenuStrip_Opening, menuItems);
		}

		private void Insert_Picture_Clicked(object sender, EventArgs e)
		{
			var owningSense = MyDataTree.CurrentSlice.Object as ILexSense ?? MyDataTree.CurrentSlice.Object.OwnerOfClass<ILexSense>();
			var app = PropertyTable.GetValue<IFlexApp>("App");
			using (var dlg = new PicturePropertiesDialog(_majorFlexComponentParameters.LcmCache, null, PropertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider"), app, true))
			{
				if (dlg.Initialize())
				{
					dlg.UseMultiStringCaption(_majorFlexComponentParameters.LcmCache, WritingSystemServices.kwsVernAnals, PropertyTable.GetValue<LcmStyleSheet>("FlexStyleSheet"));
					if (dlg.ShowDialog() == DialogResult.OK)
					{
						UndoableUnitOfWorkHelper.Do(LexiconResources.ksUndoInsertPicture, LexiconResources.ksRedoInsertPicture, owningSense, () =>
						{
							const string defaultPictureFolder = CmFolderTags.DefaultPictureFolder;
							var picture = _majorFlexComponentParameters.LcmCache.ServiceLocator.GetInstance<ICmPictureFactory>().Create();
							owningSense.PicturesOS.Add(picture);
							dlg.GetMultilingualCaptionValues(picture.Caption);
							picture.UpdatePicture(dlg.CurrentFile, null, defaultPictureFolder, 0);
						});
					}
				}
			}
		}

		private void Insert_ExtendedNote_Clicked(object sender, EventArgs e)
		{
			var owningSense = MyDataTree.CurrentSlice.Object as ILexSense ?? MyDataTree.CurrentSlice.Object.OwnerOfClass<ILexSense>();
			UndoableUnitOfWorkHelper.Do(LexiconResources.Undo_Create_Extended_Note, LexiconResources.Redo_Create_Extended_Note, owningSense, () =>
			{
				var extendedNote = _majorFlexComponentParameters.LcmCache.ServiceLocator.GetInstance<ILexExtendedNoteFactory>().Create();
				owningSense.ExtendedNoteOS.Add(extendedNote);
			});
		}

		private void MenuDataTree_SenseContextMenuStrip_Opening(object sender, CancelEventArgs e)
		{
#if RANDYTODO
			// TODO: Enable/disable menu items, based on selected slice in DataTree.
#endif
		}

		private Tuple<ContextMenuStrip, CancelEventHandler, List<Tuple<ToolStripMenuItem, EventHandler>>> CreateMainPanelContextMenuStrip(string panelMenuId)
		{
			// <menu id="PaneBar-LexicalDetail" label="">
			// <menu id="LexEntryPaneMenu" icon="MenuWidget">
			// Handled elsewhere: <item label="Show Hidden Fields" boolProperty="ShowHiddenFields-lexiconEdit" defaultVisible="true" settingsGroup="local"/>
			var contextMenuStrip = new ContextMenuStrip();
			contextMenuStrip.Opening += MainPanelContextMenuStrip_Opening;

			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>();
			var retVal = new Tuple<ContextMenuStrip, CancelEventHandler, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, MainPanelContextMenuStrip_Opening, menuItems);

			// Show_Dictionary_Preview menu item.
			_show_DictionaryPubPreviewContextMenu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Show_Dictionary_Preview_Clicked, LexiconResources.Show_DictionaryPubPreview, LexiconResources.Show_DictionaryPubPreview_ToolTip);
			_show_DictionaryPubPreviewContextMenu.Checked = PropertyTable.GetValue<bool>(Show_DictionaryPubPreview);

			// Separator
			contextMenuStrip.Items.Add(new ToolStripSeparator());

			// Insert_Sense menu item. (CmdInsertSense->msg: DataTreeInsert, also on Insert menu)
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Insert_Sense_Clicked, LexiconResources.Insert_Sense, LexiconResources.InsertSenseToolTip);

			// Insert Subsense (in sense) menu item. (CmdInsertSubsense->msg: DataTreeInsert, also on Insert menu)
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Insert_Subsense_Clicked, LexiconResources.Insert_Subsense, LexiconResources.Insert_Subsense_Tooltip);

			// Insert _Variant menu item. (CmdInsertVariant->msg: InsertItemViaBackrefVector, also on Insert menu)
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Insert_Variant_Clicked, LexiconResources.Insert_Variant, LexiconResources.Insert_Variant_Tooltip);

			// Insert A_llomorph menu item. (CmdDataTree-Insert-AlternateForm->msg: DataTreeInsert, also on Insert menu)
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Insert_Allomorph_Clicked, LexiconResources.Insert_Allomorph, LexiconResources.Insert_Allomorph_Tooltip);

			// Insert _Pronunciation menu item. (CmdDataTree-Insert-Pronunciation->msg: DataTreeInsert, also on Insert menu)
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Insert_Pronunciation_Clicked, LexiconResources.Insert_Pronunciation, LexiconResources.Insert_Pronunciation_Tooltip);

			// Insert Sound or Movie _File menu item. (CmdInsertMediaFile->msg: InsertMediaFile, also on Insert menu)
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Insert_Sound_Or_Movie_File_Clicked, LexiconResources.Insert_Sound_Or_Movie_File, LexiconResources.Insert_Sound_Or_Movie_File_Tooltip);

			// Insert _Etymology menu item. (CmdDataTree-Insert-Etymology->msg: DataTreeInsert, also on Insert menu and a hotlionks and another context menu.)
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Insert_Etymology_Clicked, LexiconResources.Insert_Etymology, LexiconResources.Insert_Etymology_Tooltip);

			// Separator
			contextMenuStrip.Items.Add(new ToolStripSeparator());

			// Lexeme Form has components. (CmdChangeToComplexForm->msg: ConvertEntryIntoVariant)
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Lexeme_Form_Has_Components_Clicked, LexiconResources.Lexeme_Form_Has_Components, LexiconResources.Lexeme_Form_Has_Components_Tooltip);

			// Lexeme Form is a variant menu item. (CmdChangeToVariant->msg: ConvertEntryIntoComplexForm)
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Lexeme_Form_Is_A_Variant_Clicked, LexiconResources.Lexeme_Form_Is_A_Variant, LexiconResources.Lexeme_Form_Is_A_Variant_Tooltip);

			// _Merge with entry... menu item. (CmdMergeEntry->msg: MergeEntry, also on Tool menu)
			var contextMenuItem = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Merge_With_Entry_Clicked, LexiconResources.Merge_With_Entry, LexiconResources.Merge_With_Entry_Tooltip);
			// Original code that controlled: display.Enabled = display.Visible = InFriendlyArea;
			// It is now only in a friendly area, so should always be visible and enabled, per the old code.
			// Trouble is it makes no sense to enable it if the lexicon only has one entry in it, so I'll alter the behavior to be more sensible. ;-)
			contextMenuItem.Enabled = PropertyTable.GetValue<LcmCache>("cache").LanguageProject.LexDbOA.Entries.Any();

			// Separator
			contextMenuStrip.Items.Add(new ToolStripSeparator());

			// Show Entry in Concordance menu item. (CmdRootEntryJumpToConcordance->msg: JumpToTool, also on Insert menu)
			ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Show_Entry_In_Concordance_Clicked, LexiconResources.Show_Entry_In_Concordance);

			return retVal;
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
				var currentObject = currentSlice.Object;
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
				LexSenseUi.CreateNewLexSense(_majorFlexComponentParameters.LcmCache, owningSense, owningSense.SensesOS.IndexOf(currentSense) + 1);
			}
			else
			{
				var owningEntry = (ILexEntry)MyRecordList.CurrentObject;
				LexSenseUi.CreateNewLexSense(_majorFlexComponentParameters.LcmCache, owningEntry, owningEntry.SensesOS.IndexOf(currentSense) + 1);
			}
		}

		private void MainPanelContextMenuStrip_Opening(object sender, CancelEventArgs e)
		{
#if RANDYTODO
			// TODO: Enable/disable menu items, based on selected slice in DataTree.
#endif
		}

		private void Show_Entry_In_Concordance_Clicked(object sender, EventArgs e)
		{
			MessageBox.Show((Form)_majorFlexComponentParameters.MainWindow, "Show Entry In Concordance...");
		}

		private void Delete_Sense_Clicked(object sender, EventArgs e)
		{
			MyDataTree.CurrentSlice.HandleDeleteCommand();
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
				var window = PropertyTable.GetValue<Form>("window");
				dlg.InitializeFlexComponent(new FlexComponentParameters(PropertyTable, Publisher, Subscriber));
				// <parameters title="Merge Entry" formlabel="_Find:" okbuttonlabel="_Merge"/>
				dlg.SetDlgInfo(_majorFlexComponentParameters.LcmCache, XElement.Parse(LexiconResources.MatchingEntriesParameters), currentEntry, LexiconResources.ksMergeEntry, FwUtils.ReplaceUnderlineWithAmpersand(LexiconResources.ks_Find), FwUtils.ReplaceUnderlineWithAmpersand(LexiconResources.ks_Merge));
				if (dlg.ShowDialog(window) != DialogResult.OK)
				{
					return;
				}

				var survivor = (ILexEntry)dlg.SelectedObject;
				Debug.Assert(survivor != currentEntry);
				UndoableUnitOfWorkHelper.Do(LexiconResources.ksUndoMergeEntry, LexiconResources.ksRedoMergeEntry, _majorFlexComponentParameters.LcmCache.ActionHandlerAccessor, () =>
				{
					survivor.MergeObject(currentEntry, true);
					survivor.DateModified = DateTime.Now;
				});
				MessageBox.Show(window, LexiconResources.ksEntriesHaveBeenMerged, LexiconResources.ksMergeReport, MessageBoxButtons.OK, MessageBoxIcon.Information);
				var commands = new List<string>
				{
					"AboutToFollowLink",
					"FollowLink"
				};
				var parms = new List<object>
				{
					null,
					survivor.Hvo
				};
				Publisher.Publish(commands, parms);
			}
		}

		private void Lexeme_Form_Is_A_Variant_Clicked(object sender, EventArgs e)
		{
			MessageBox.Show((Form)_majorFlexComponentParameters.MainWindow, "Lexeme Form Is A Variant...");
		}

		private void Lexeme_Form_Has_Components_Clicked(object sender, EventArgs e)
		{
			MessageBox.Show((Form)_majorFlexComponentParameters.MainWindow, "Lexeme Form Has Components...");
		}

		private void Insert_Sound_Or_Movie_File_Clicked(object sender, EventArgs e)
		{
			const string insertMediaFileLastDirectory = "InsertMediaFile-LastDirectory";
			var cache = _majorFlexComponentParameters.LcmCache;
			var lexEntry = (ILexEntry)MyRecordList.CurrentObject;
			var createdMediaFile = false;
			using (var unitOfWorkHelper = new UndoableUnitOfWorkHelper(_majorFlexComponentParameters.LcmCache.ActionHandlerAccessor, LexiconResources.ksUndoInsertMedia, LexiconResources.ksRedoInsertMedia))
			{
				if (!lexEntry.PronunciationsOS.Any())
				{
					// Ensure that the pronunciation writing systems have been initialized.
					// Otherwise, the crash reported in FWR-2086 can happen!
					lexEntry.PronunciationsOS.Add(cache.ServiceLocator.GetInstance<ILexPronunciationFactory>().Create());
				}
				var firstPronunciation = lexEntry.PronunciationsOS[0];
				using (var dlg = new OpenFileDialogAdapter())
				{
					dlg.InitialDirectory = PropertyTable.GetValue(insertMediaFileLastDirectory, cache.LangProject.LinkedFilesRootDir);
					dlg.Filter = ResourceHelper.BuildFileFilter(FileFilterType.AllAudio, FileFilterType.AllVideo, FileFilterType.AllFiles);
					dlg.FilterIndex = 1;
					if (string.IsNullOrEmpty(dlg.Title) || dlg.Title == "*kstidInsertMediaChooseFileCaption*")
					{
						dlg.Title = LexiconResources.ChooseSoundOrMovieFile;
					}
					dlg.RestoreDirectory = true;
					dlg.CheckFileExists = true;
					dlg.CheckPathExists = true;
					dlg.Multiselect = true;

					var dialogResult = DialogResult.None;
					var helpProvider = PropertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider");
					var linkedFilesRootDir = cache.LangProject.LinkedFilesRootDir;
					var mediaFactory = cache.ServiceLocator.GetInstance<ICmMediaFactory>();
					while (dialogResult != DialogResult.OK && dialogResult != DialogResult.Cancel)
					{
						dialogResult = dlg.ShowDialog();
						if (dialogResult == DialogResult.OK)
						{
							var fileNames = MoveOrCopyFilesController.MoveCopyOrLeaveMediaFiles(dlg.FileNames, linkedFilesRootDir, helpProvider);
							var mediaFolderName = StringTable.Table.GetString("kstidMediaFolder");
							if (string.IsNullOrEmpty(mediaFolderName) || mediaFolderName == "*kstidMediaFolder*")
							{
								mediaFolderName = CmFolderTags.LocalMedia;
							}
							foreach (var fileName in fileNames.Where(f => !string.IsNullOrEmpty(f)))
							{
								var media = mediaFactory.Create();
								firstPronunciation.MediaFilesOS.Add(media);
								media.MediaFileRA = DomainObjectServices.FindOrCreateFile(DomainObjectServices.FindOrCreateFolder(cache, LangProjectTags.kflidMedia, mediaFolderName), fileName);
							}
							createdMediaFile = true;
							var selectedFileName = dlg.FileNames.FirstOrDefault(f => !string.IsNullOrEmpty(f));
							if (selectedFileName != null)
							{
								PropertyTable.SetProperty(insertMediaFileLastDirectory, Path.GetDirectoryName(selectedFileName), true, false);
							}
						}
					}
					// If we didn't create any ICmMedia instances, then roll back the UOW, even if it created a new ILexPronunciation.
					unitOfWorkHelper.RollBack = !createdMediaFile;
				}
			}
		}

		private void Insert_Pronunciation_Clicked(object sender, EventArgs e)
		{
			var lexEntry = (ILexEntry)MyRecordList.CurrentObject;
			UndoableUnitOfWorkHelper.Do(LcmUiStrings.ksUndoInsert, LcmUiStrings.ksRedoInsert, _majorFlexComponentParameters.LcmCache.ServiceLocator.GetInstance<IActionHandler>(), () =>
			{
				_majorFlexComponentParameters.LcmCache.DomainDataByFlid.MakeNewObject(LexPronunciationTags.kClassId, lexEntry.Hvo, LexEntryTags.kflidPronunciations, lexEntry.PronunciationsOS.Count);
				// Forces them to be created (lest it try to happen while displaying the new object in PropChanged).
				var dummy = _majorFlexComponentParameters.LcmCache.LangProject.DefaultPronunciationWritingSystem;
			});
		}

		private void Insert_Allomorph_Clicked(object sender, EventArgs e)
		{
			var lexEntry = (ILexEntry)MyRecordList.CurrentObject;
			UndoableUnitOfWorkHelper.Do(LcmUiStrings.ksUndoInsert, LcmUiStrings.ksRedoInsert, _majorFlexComponentParameters.LcmCache.ServiceLocator.GetInstance<IActionHandler>(), () =>
			{
				_majorFlexComponentParameters.LcmCache.DomainDataByFlid.MakeNewObject(lexEntry.GetDefaultClassForNewAllomorph(), lexEntry.Hvo, LexEntryTags.kflidAlternateForms, lexEntry.AlternateFormsOS.Count);
			});
		}

		private void Insert_Variant_Clicked(object sender, EventArgs e)
		{
			using (var dlg = new InsertVariantDlg())
			{
				dlg.InitializeFlexComponent(_majorFlexComponentParameters.FlexComponentParameters);
				var entOld = (ILexEntry)MyDataTree.Root;
				dlg.SetHelpTopic("khtpInsertVariantDlg");
				dlg.SetDlgInfo(_majorFlexComponentParameters.LcmCache, entOld);
				dlg.ShowDialog();
			}
		}

		private void Insert_Subsense_Clicked(object sender, EventArgs e)
		{
			var owningSense = MyDataTree.CurrentSlice.Object as ILexSense ?? MyDataTree.CurrentSlice.Object.OwnerOfClass<ILexSense>();
			LexSenseUi.CreateNewLexSense(_majorFlexComponentParameters.LcmCache, owningSense);
		}

		private void Insert_Sense_Clicked(object sender, EventArgs e)
		{
			LexSenseUi.CreateNewLexSense(_majorFlexComponentParameters.LcmCache, (ILexEntry)MyRecordList.CurrentObject);
		}

		private void Insert_Entry_Clicked(object sender, EventArgs e)
		{
			using (var dlg = new InsertEntryDlg())
			{
				var mainWindow = PropertyTable.GetValue<IFwMainWnd>("window");
				dlg.InitializeFlexComponent(new FlexComponentParameters(PropertyTable, Publisher, Subscriber));
				dlg.SetDlgInfo(_majorFlexComponentParameters.LcmCache, PersistenceProviderFactory.CreatePersistenceProvider(PropertyTable));
				if (dlg.ShowDialog((Form) mainWindow) != DialogResult.OK)
				{
					return;
				}
				ILexEntry entry;
				bool newby;
				dlg.GetDialogInfo(out entry, out newby);
				// No need for a PropChanged here because InsertEntryDlg takes care of that. (LT-3608)
				mainWindow.RefreshAllViews();
				MyRecordList.JumpToRecord(entry.Hvo);
			}
		}

		private void GoToEntry_Clicked(object sender, EventArgs e)
		{
			using (var dlg = new EntryGoDlg())
			{
				dlg.InitializeFlexComponent(new FlexComponentParameters(PropertyTable, Publisher, Subscriber));
				var windowParameters = new WindowParams
				{
					m_btnText = FwUtils.ReplaceUnderlineWithAmpersand(LexiconResources.Go),
					m_label = FwUtils.ReplaceUnderlineWithAmpersand(LexiconResources.Go_To),
					m_title = LexiconResources.Go_To_Entry_Dlg_Title
				};
				dlg.SetDlgInfo(_majorFlexComponentParameters.LcmCache, windowParameters);
				dlg.SetHelpTopic("khtpFindLexicalEntry");
				if (dlg.ShowDialog(PropertyTable.GetValue<Form>("window")) == DialogResult.OK)
				{
					MyRecordList.JumpToRecord(dlg.SelectedObject.Hvo);
				}
			}
		}

		private void Show_Dictionary_Preview_Clicked(object sender, EventArgs e)
		{
			var menuItem = (ToolStripMenuItem)sender;
			_show_DictionaryPubPreviewMenu.Checked = !menuItem.Checked;
			_show_DictionaryPubPreviewContextMenu.Checked = !menuItem.Checked;
			PropertyTable.SetProperty(Show_DictionaryPubPreview, menuItem.Checked, SettingsGroup.LocalSettings, true, false);
			InnerMultiPane.Panel1Collapsed = !menuItem.Checked;
		}

		private void Show_Hidden_Fields_Clicked(object sender, EventArgs e)
		{
			var menuItem = (ToolStripMenuItem)sender;
			menuItem.Checked = !menuItem.Checked;
			PropertyTable.SetProperty(_extendedPropertyName, menuItem.Checked, SettingsGroup.LocalSettings, true, false);
			Publisher.Publish("ShowHiddenFields", menuItem.Checked);
			InnerMultiPane.Panel1Collapsed = !menuItem.Checked;
		}

		private void AddToolbarItems()
		{
			// <item command="CmdInsertLexEntry" defaultVisible="false" />
			_insertEntryToolStripButton = ToolStripButtonFactory.CreateToolStripButton(Insert_Entry_Clicked, "toolStripButtonInsertEntry", LexiconResources.Major_Entry.ToBitmap(), LexiconResources.Entry_Tooltip);
			// <item command="CmdGoToEntry" defaultVisible="false" />
			_insertGoToEntryToolStripButton = ToolStripButtonFactory.CreateToolStripButton(GoToEntry_Clicked, "toolStripButtonGoToEntry", LexiconResources.Find_Lexical_Entry.ToBitmap(), LexiconResources.GoToEntryToolTip);

			InsertToolbarManager.AddInsertToolbarItems(_majorFlexComponentParameters, new List<ToolStripButton> { _insertEntryToolStripButton, _insertGoToEntryToolStripButton });
		}

		private void AddEditMenuItems()
		{
			_editMenu = MenuServices.GetEditMenu(_majorFlexComponentParameters.MenuStrip);
			// Insert before third separator menu
			// <item command="CmdGoToEntry" />
			ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(_newEditMenusAndHandlers, _editMenu, GoToEntry_Clicked, LexiconResources.Find_Entry, LexiconResources.GoToEntryToolTip, Keys.Control | Keys.F, LexiconResources.Find_Lexical_Entry.ToBitmap(), 10);
		}

		private void AddViewMenuItems()
		{
			_viewMenu = MenuServices.GetViewMenu(_majorFlexComponentParameters.MenuStrip);
			// <item label="Show _Dictionary Preview" boolProperty="Show_DictionaryPubPreview" defaultVisible="false"/>
			_show_DictionaryPubPreviewMenu = ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(_newViewMenusAndHandlers, _viewMenu, Show_Dictionary_Preview_Clicked, LexiconResources.Show_DictionaryPubPreview, insertIndex: _viewMenu.DropDownItems.Count - 2);
			_show_DictionaryPubPreviewMenu.Checked = PropertyTable.GetValue<bool>(Show_DictionaryPubPreview);
			// <item label="_Show Hidden Fields" boolProperty="ShowHiddenFields" defaultVisible="false"/>
			_showHiddenFieldsMenu = ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(_newViewMenusAndHandlers, _viewMenu, Show_Hidden_Fields_Clicked, LanguageExplorerResources.ksShowHiddenFields, insertIndex: _viewMenu.DropDownItems.Count - 2);
			_showHiddenFieldsMenu.Checked = PropertyTable.GetValue(_extendedPropertyName, false);
		}

		private void AddInsertMenuItems()
		{
			_insertMenu = MenuServices.GetInsertMenu(_majorFlexComponentParameters.MenuStrip);

			var insertIndex = 0;
			// <item command="CmdInsertLexEntry" defaultVisible="false" />
			ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(_newInsertMenusAndHandlers, _insertMenu, Insert_Entry_Clicked, LexiconResources.Entry, LexiconResources.Entry_Tooltip, Keys.Control | Keys.E, LexiconResources.Major_Entry.ToBitmap(), insertIndex);
			// <item command="CmdInsertSense" defaultVisible="false" />
			ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(_newInsertMenusAndHandlers, _insertMenu, Insert_Sense_Clicked, LexiconResources.Sense, LexiconResources.InsertSenseToolTip, insertIndex: ++insertIndex);
			// <item command="CmdInsertVariant" defaultVisible="false" />
			ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(_newInsertMenusAndHandlers, _insertMenu, Insert_Variant_Clicked, LexiconResources.Variant, LexiconResources.Insert_Variant_Tooltip, insertIndex: ++insertIndex);
			// <item command="CmdDataTree-Insert-AlternateForm" label="A_llomorph" defaultVisible="false" />
			ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(_newInsertMenusAndHandlers, _insertMenu, Insert_Allomorph_Clicked, LexiconResources.Allomorph, LexiconResources.Insert_Allomorph_Tooltip, insertIndex: ++insertIndex);
			// <item command="CmdDataTree-Insert-Pronunciation" defaultVisible="false" />
			ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(_newInsertMenusAndHandlers, _insertMenu, Insert_Pronunciation_Clicked, LexiconResources.Pronunciation, LexiconResources.Insert_Pronunciation_Tooltip, insertIndex: ++insertIndex);
			// <item command="CmdInsertMediaFile" defaultVisible="false" />
			ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(_newInsertMenusAndHandlers, _insertMenu, Insert_Sound_Or_Movie_File_Clicked, LexiconResources.Sound_or_Movie, LexiconResources.Insert_Sound_Or_Movie_File_Tooltip, insertIndex: ++insertIndex);
			//<item command="CmdDataTree-Insert-Etymology" defaultVisible="false" />
			ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(_newInsertMenusAndHandlers, _insertMenu, Insert_Etymology_Clicked, LexiconResources.Etymology, LexiconResources.Insert_Etymology_Tooltip, Keys.None, null, ++insertIndex);

			// <item label="-" translate="do not translate" />
			ToolStripItem senseMenuItem = ToolStripMenuItemFactory.CreateToolStripSeparatorForToolStripMenuItem(_insertMenu, ++insertIndex);
			senseMenuItem.Visible = false;
			_senseMenuItems.Add(senseMenuItem);

			// <item command="CmdInsertSubsense" defaultVisible="false" />
			senseMenuItem = ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(_newInsertMenusAndHandlers, _insertMenu, Insert_Subsense_Clicked, LexiconResources.SubsenseInSense, LexiconResources.Insert_Subsense_Tooltip, insertIndex: ++insertIndex);
			senseMenuItem.Visible = false;
			_senseMenuItems.Add(senseMenuItem);

			// <item command="CmdInsertPicture" defaultVisible="false" />
			senseMenuItem = ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(_newInsertMenusAndHandlers, _insertMenu, Insert_Picture_Clicked, LexiconResources.Picture, LexiconResources.Insert_Picture_Tooltip, insertIndex: ++insertIndex);
			senseMenuItem.Visible = false;
			_senseMenuItems.Add(senseMenuItem);

			// <item command="CmdInsertExtNote" defaultVisible="false" />
			senseMenuItem = ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(_newInsertMenusAndHandlers, _insertMenu, Insert_ExtendedNote_Clicked, LexiconResources.ExtendedNote, insertIndex: ++insertIndex);
			senseMenuItem.Visible = false;
			_senseMenuItems.Add(senseMenuItem);
		}

		private void AddToolsMenuItems()
		{
			var insertIndex = -1;

			// <command id="CmdConfigureDictionary" label="Configure {0}" message="ConfigureDictionary"/>
			// <item label="{0}" command="CmdConfigureDictionary" defaultVisible="false"/>
			_toolsConfigureMenu = MenuServices.GetToolsConfigureMenu(_majorFlexComponentParameters.MenuStrip);
			ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(_newToolsConfigurationMenusAndHandlers, _toolsConfigureMenu, Tools_Configure_Dictionary_Clicked, AreaResources.ConfigureDictionary, insertIndex: ++insertIndex);

			// <item command="CmdConfigureColumns" defaultVisible="false" />
			_lexiconAreaMenuHelper.MyAreaWideMenuHelper.SetupToolsConfigureColumnsMenu(RecordBrowseView.BrowseViewer, ++insertIndex);

			// <item command="CmdMergeEntry" defaultVisible="false"/>
			// First add separator.
			insertIndex = 0;
			_toolsMenu = MenuServices.GetToolsMenu(_majorFlexComponentParameters.MenuStrip);
			_toolMenuToolStripSeparator = ToolStripMenuItemFactory.CreateToolStripSeparatorForToolStripMenuItem(_toolsMenu, ++insertIndex);
			ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(_newToolsMenusAndHandlers, _toolsMenu, Merge_With_Entry_Clicked, LexiconResources.MergeWithEntry, LexiconResources.Merge_With_Entry_Tooltip, insertIndex: ++insertIndex);
		}

		private void Tools_Configure_Dictionary_Clicked(object sender, EventArgs e)
		{
			var mainWindow = PropertyTable.GetValue<IFwMainWnd>("window");
			if (DictionaryConfigurationDlg.ShowDialog(_majorFlexComponentParameters.FlexComponentParameters, (Form)mainWindow, MyRecordList.CurrentObject, "khtpConfigureDictionary", LanguageExplorerResources.Dictionary))
			{
				mainWindow.RefreshAllViews();
			}
		}
	}
}