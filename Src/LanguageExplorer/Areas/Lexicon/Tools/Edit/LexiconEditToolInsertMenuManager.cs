// Copyright (c) 2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
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
	/// Implementation that supports the addition(s) to FLEx's main Insert menu for the Lexicon Edit tool.
	/// </summary>
	internal sealed class LexiconEditToolInsertMenuManager : IToolUiWidgetManager
	{
		private IRecordList MyRecordList { get; set; }
		private Dictionary<string, EventHandler> _sharedEventHandlers;
		private FlexComponentParameters _flexComponentParameters;
		private IPropertyTable _propertyTable;
		private LcmCache _cache;
		private IFwMainWnd _mainWnd;
		private ToolStripMenuItem _insertMenu;
		private List<Tuple<ToolStripMenuItem, EventHandler>> _newInsertMenusAndHandlers = new List<Tuple<ToolStripMenuItem, EventHandler>>();
		private List<ToolStripItem> _senseMenuItems = new List<ToolStripItem>();
		private DataTree MyDataTree { get; set; }

		#region IToolUiWidgetManager

		/// <inheritdoc />
		void IToolUiWidgetManager.Initialize(MajorFlexComponentParameters majorFlexComponentParameters, IRecordList recordList, IReadOnlyDictionary<string, EventHandler> sharedEventHandlers, IReadOnlyList<object> randomParameters)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
			Guard.AgainstNull(recordList, nameof(recordList));
			Guard.AgainstNull(randomParameters, nameof(randomParameters));
			Guard.AssertThat(randomParameters.Count == 1, "Wrong number of random parameters.");

			_flexComponentParameters = majorFlexComponentParameters.FlexComponentParameters;
			_propertyTable = majorFlexComponentParameters.FlexComponentParameters.PropertyTable;
			_cache = majorFlexComponentParameters.LcmCache;
			_mainWnd = majorFlexComponentParameters.MainWindow;
			MyRecordList = recordList;
			MyDataTree = (DataTree)randomParameters[0];
			MyDataTree.CurrentSliceChanged += MyDataTree_CurrentSliceChanged;

			_insertMenu = MenuServices.GetInsertMenu(majorFlexComponentParameters.MenuStrip);

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

		/// <inheritdoc />
		IReadOnlyDictionary<string, EventHandler> IToolUiWidgetManager.SharedEventHandlers => _sharedEventHandlers ?? (_sharedEventHandlers = new Dictionary<string, EventHandler>(10)
		{
			{ LexiconEditToolConstants.CmdInsertLexEntry, Insert_Entry_Clicked },
			{ LexiconEditToolConstants.CmdInsertSense, Insert_Sense_Clicked },
			{ LexiconEditToolConstants.CmdInsertSubsense, Insert_Subsense_Clicked },
			{ LexiconEditToolConstants.CmdDataTree_Insert_AlternateForm, Insert_Allomorph_Clicked },
			{ LexiconEditToolConstants.CmdDataTree_Insert_Etymology, Insert_Etymology_Clicked },
			{ LexiconEditToolConstants.CmdDataTree_Insert_Pronunciation, Insert_Pronunciation_Clicked },
			{ LexiconEditToolConstants.CmdInsertExtNote, Insert_ExtendedNote_Clicked },
			{ LexiconEditToolConstants.CmdInsertPicture, Insert_Picture_Clicked },
			{ LexiconEditToolConstants.CmdInsertVariant, Insert_Variant_Clicked },
			{ LexiconEditToolConstants.CmdInsertMediaFile, Insert_Sound_Or_Movie_File_Clicked }
		});

		#endregion

		#region IDisposable

		private bool _isDisposed;

		~LexiconEditToolInsertMenuManager()
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
				MyDataTree.CurrentSliceChanged -= MyDataTree_CurrentSliceChanged;
				_senseMenuItems.Clear();

				foreach (var menuTuple in _newInsertMenusAndHandlers)
				{
					menuTuple.Item1.Click -= menuTuple.Item2;
					_insertMenu.DropDownItems.Remove(menuTuple.Item1);
					menuTuple.Item1.Dispose();
				}
				_newInsertMenusAndHandlers.Clear();
				_sharedEventHandlers.Clear();
			}
			MyRecordList = null;
			_sharedEventHandlers = null;
			_flexComponentParameters = null;
			_propertyTable = null;
			_cache = null;
			_mainWnd = null;
			_insertMenu = null;
			_newInsertMenusAndHandlers = null;
			_senseMenuItems = null;
			MyDataTree = null;

			_isDisposed = true;
		}

		#endregion

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

		private void Insert_Entry_Clicked(object sender, EventArgs e)
		{
			using (var dlg = new InsertEntryDlg())
			{
				dlg.InitializeFlexComponent(_flexComponentParameters);
				dlg.SetDlgInfo(_cache, PersistenceProviderFactory.CreatePersistenceProvider(_propertyTable));
				if (dlg.ShowDialog((Form)_mainWnd) != DialogResult.OK)
				{
					return;
				}
				ILexEntry entry;
				bool newby;
				dlg.GetDialogInfo(out entry, out newby);
				// No need for a PropChanged here because InsertEntryDlg takes care of that. (LT-3608)
				_mainWnd.RefreshAllViews();
				MyRecordList.JumpToRecord(entry.Hvo);
			}
		}

		private void Insert_Sense_Clicked(object sender, EventArgs e)
		{
			LexSenseUi.CreateNewLexSense(_cache, (ILexEntry)MyRecordList.CurrentObject);
		}

		private void Insert_Subsense_Clicked(object sender, EventArgs e)
		{
			var owningSense = MyDataTree.CurrentSlice.Object as ILexSense ?? MyDataTree.CurrentSlice.Object.OwnerOfClass<ILexSense>();
			LexSenseUi.CreateNewLexSense(_cache, owningSense);
		}

		private void Insert_Variant_Clicked(object sender, EventArgs e)
		{
			using (var dlg = new InsertVariantDlg())
			{
				dlg.InitializeFlexComponent(_flexComponentParameters);
				var entOld = (ILexEntry)MyDataTree.Root;
				dlg.SetHelpTopic("khtpInsertVariantDlg");
				dlg.SetDlgInfo(_cache, entOld);
				dlg.ShowDialog();
			}
		}

		private void Insert_Allomorph_Clicked(object sender, EventArgs e)
		{
			var lexEntry = (ILexEntry)MyRecordList.CurrentObject;
			UndoableUnitOfWorkHelper.Do(LcmUiStrings.ksUndoInsert, LcmUiStrings.ksRedoInsert, _cache.ServiceLocator.GetInstance<IActionHandler>(), () =>
			{
				_cache.DomainDataByFlid.MakeNewObject(lexEntry.GetDefaultClassForNewAllomorph(), lexEntry.Hvo, LexEntryTags.kflidAlternateForms, lexEntry.AlternateFormsOS.Count);
			});
		}

		private void Insert_Pronunciation_Clicked(object sender, EventArgs e)
		{
			var lexEntry = (ILexEntry)MyRecordList.CurrentObject;
			UndoableUnitOfWorkHelper.Do(LcmUiStrings.ksUndoInsert, LcmUiStrings.ksRedoInsert, _cache.ServiceLocator.GetInstance<IActionHandler>(), () =>
			{
				_cache.DomainDataByFlid.MakeNewObject(LexPronunciationTags.kClassId, lexEntry.Hvo, LexEntryTags.kflidPronunciations, lexEntry.PronunciationsOS.Count);
				// Forces them to be created (lest it try to happen while displaying the new object in PropChanged).
				var dummy = _cache.LangProject.DefaultPronunciationWritingSystem;
			});
		}

		private void Insert_Sound_Or_Movie_File_Clicked(object sender, EventArgs e)
		{
			const string insertMediaFileLastDirectory = "InsertMediaFile-LastDirectory";
			var lexEntry = (ILexEntry)MyRecordList.CurrentObject;
			var createdMediaFile = false;
			using (var unitOfWorkHelper = new UndoableUnitOfWorkHelper(_cache.ActionHandlerAccessor, LexiconResources.ksUndoInsertMedia, LexiconResources.ksRedoInsertMedia))
			{
				if (!lexEntry.PronunciationsOS.Any())
				{
					// Ensure that the pronunciation writing systems have been initialized.
					// Otherwise, the crash reported in FWR-2086 can happen!
					lexEntry.PronunciationsOS.Add(_cache.ServiceLocator.GetInstance<ILexPronunciationFactory>().Create());
				}
				var firstPronunciation = lexEntry.PronunciationsOS[0];
				using (var dlg = new OpenFileDialogAdapter())
				{
					dlg.InitialDirectory = _propertyTable.GetValue(insertMediaFileLastDirectory, _cache.LangProject.LinkedFilesRootDir);
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
					var helpProvider = _propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider");
					var linkedFilesRootDir = _cache.LangProject.LinkedFilesRootDir;
					var mediaFactory = _cache.ServiceLocator.GetInstance<ICmMediaFactory>();
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
								media.MediaFileRA = DomainObjectServices.FindOrCreateFile(DomainObjectServices.FindOrCreateFolder(_cache, LangProjectTags.kflidMedia, mediaFolderName), fileName);
							}
							createdMediaFile = true;
							var selectedFileName = dlg.FileNames.FirstOrDefault(f => !string.IsNullOrEmpty(f));
							if (selectedFileName != null)
							{
								_propertyTable.SetProperty(insertMediaFileLastDirectory, Path.GetDirectoryName(selectedFileName), true);
							}
						}
					}
					// If we didn't create any ICmMedia instances, then roll back the UOW, even if it created a new ILexPronunciation.
					unitOfWorkHelper.RollBack = !createdMediaFile;
				}
			}
		}

		private void Insert_Etymology_Clicked(object sender, EventArgs e)
		{
			UndoableUnitOfWorkHelper.Do(LexiconResources.Undo_Insert_Etymology, LexiconResources.Redo_Insert_Etymology, _cache.ServiceLocator.GetInstance<IActionHandler>(), () =>
			{
				((ILexEntry)MyRecordList.CurrentObject).EtymologyOS.Add(_cache.ServiceLocator.GetInstance<ILexEtymologyFactory>().Create());
			});
		}

		private void Insert_Picture_Clicked(object sender, EventArgs e)
		{
			var owningSense = MyDataTree.CurrentSlice.Object as ILexSense ?? MyDataTree.CurrentSlice.Object.OwnerOfClass<ILexSense>();
			var app = _propertyTable.GetValue<IFlexApp>("App");
			using (var dlg = new PicturePropertiesDialog(_cache, null, _propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider"), app, true))
			{
				if (dlg.Initialize())
				{
					dlg.UseMultiStringCaption(_cache, WritingSystemServices.kwsVernAnals, _propertyTable.GetValue<LcmStyleSheet>("FlexStyleSheet"));
					if (dlg.ShowDialog() == DialogResult.OK)
					{
						UndoableUnitOfWorkHelper.Do(LexiconResources.ksUndoInsertPicture, LexiconResources.ksRedoInsertPicture, owningSense, () =>
						{
							const string defaultPictureFolder = CmFolderTags.DefaultPictureFolder;
							var picture = _cache.ServiceLocator.GetInstance<ICmPictureFactory>().Create();
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
				var extendedNote = _cache.ServiceLocator.GetInstance<ILexExtendedNoteFactory>().Create();
				owningSense.ExtendedNoteOS.Add(extendedNote);
			});
		}
	}
}