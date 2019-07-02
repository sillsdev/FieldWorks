// Copyright (c) 2015-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Controls;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Areas.Grammar.Tools.BulkEditPhonemes
{
	/// <summary>
	/// ITool implementation for the "bulkEditPhonemes" tool in the "grammar" area.
	/// </summary>
	[Export(AreaServices.GrammarAreaMachineName, typeof(ITool))]
	internal sealed class BulkEditPhonemesTool : ITool
	{
		private BulkEditPhonemesToolMenuHelper _toolMenuHelper;
		private PaneBarContainer _paneBarContainer;
		private AssignFeaturesToPhonemes _assignFeaturesToPhonemesView;
		private IRecordList _recordList;
		[Import(AreaServices.GrammarAreaMachineName)]
		private IArea _area;

		#region Implementation of IMajorFlexComponent

		/// <summary>
		/// Deactivate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the outgoing component, when the user switches to a component.
		/// </remarks>
		public void Deactivate(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			// This will also remove any event handlers set up by the tool's UserControl instances that may have registered event handlers.
			majorFlexComponentParameters.UiWidgetController.RemoveToolHandlers();
			_toolMenuHelper.Dispose();
			PaneBarContainerFactory.RemoveFromParentAndDispose(majorFlexComponentParameters.MainCollapsingSplitContainer, ref _paneBarContainer);
			_assignFeaturesToPhonemesView = null;
			_toolMenuHelper = null;
		}

		/// <summary>
		/// Activate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the component that is becoming active.
		/// </remarks>
		public void Activate(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			if (majorFlexComponentParameters.LcmCache.LanguageProject.PhonologicalDataOA.PhonemeSetsOS.Count == 0)
			{
				// Pathological...this helps the memory-only backend mainly, but makes others self-repairing.
				NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(majorFlexComponentParameters.LcmCache.ActionHandlerAccessor, () =>
				{
					majorFlexComponentParameters.LcmCache.LanguageProject.PhonologicalDataOA.PhonemeSetsOS.Add(majorFlexComponentParameters.LcmCache.ServiceLocator.GetInstance<IPhPhonemeSetFactory>().Create());
				});
			}
			if (_recordList == null)
			{
				_recordList = majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue<IRecordListRepositoryForTools>(LanguageExplorerConstants.RecordListRepository).GetRecordList(GrammarArea.Phonemes, majorFlexComponentParameters.StatusBar, GrammarArea.PhonemesFactoryMethod);
			}
			_assignFeaturesToPhonemesView = new AssignFeaturesToPhonemes(XDocument.Parse(GrammarResources.BulkEditPhonemesToolParameters).Root, majorFlexComponentParameters.LcmCache, _recordList, majorFlexComponentParameters.UiWidgetController);
			_toolMenuHelper = new BulkEditPhonemesToolMenuHelper(majorFlexComponentParameters, this, _assignFeaturesToPhonemesView, _recordList);
			_paneBarContainer = PaneBarContainerFactory.Create(majorFlexComponentParameters.FlexComponentParameters, majorFlexComponentParameters.MainCollapsingSplitContainer, _assignFeaturesToPhonemesView);
		}

		/// <summary>
		/// Do whatever might be needed to get ready for a refresh.
		/// </summary>
		public void PrepareToRefresh()
		{
			_assignFeaturesToPhonemesView.BrowseViewer.BrowseView.PrepareToRefresh();
		}

		/// <summary>
		/// Finish the refresh.
		/// </summary>
		public void FinishRefresh()
		{
			_recordList.ReloadIfNeeded();
			((DomainDataByFlidDecoratorBase)_recordList.VirtualListPublisher).Refresh();
		}

		/// <summary>
		/// The properties are about to be saved, so make sure they are all current.
		/// Add new ones, as needed.
		/// </summary>
		public void EnsurePropertiesAreCurrent()
		{
		}

		#endregion

		#region Implementation of IMajorFlexUiComponent

		/// <summary>
		/// Get the internal name of the component.
		/// </summary>
		/// <remarks>NB: This is the machine friendly name, not the user friendly name.</remarks>
		public string MachineName => AreaServices.BulkEditPhonemesMachineName;

		/// <summary>
		/// User-visible localized component name.
		/// </summary>
		public string UiName => StringTable.Table.LocalizeLiteralValue(AreaServices.BulkEditPhonemesUiName);

		#endregion

		#region Implementation of ITool

		/// <summary>
		/// Get the area for the tool.
		/// </summary>
		public IArea Area => _area;

		/// <summary>
		/// Get the image for the area.
		/// </summary>
		public Image Icon => Images.BrowseView.SetBackgroundColor(Color.Magenta);

		#endregion

		private sealed class BulkEditPhonemesToolMenuHelper : IDisposable
		{
			private MajorFlexComponentParameters _majorFlexComponentParameters;
			private AssignFeaturesToPhonemes _assignFeaturesToPhonemesView;
			private IRecordList _recordList;

			internal BulkEditPhonemesToolMenuHelper(MajorFlexComponentParameters majorFlexComponentParameters, ITool tool, AssignFeaturesToPhonemes assignFeaturesToPhonemes, IRecordList recordList)
			{
				Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
				Guard.AgainstNull(tool, nameof(tool));
				Guard.AgainstNull(assignFeaturesToPhonemes, nameof(assignFeaturesToPhonemes));
				Guard.AgainstNull(recordList, nameof(recordList));

				_majorFlexComponentParameters = majorFlexComponentParameters;
				_assignFeaturesToPhonemesView = assignFeaturesToPhonemes;
				_recordList = recordList;
				// Tool must be added, even when it adds no tool specific handlers.
				_majorFlexComponentParameters.UiWidgetController.AddHandlers(new ToolUiWidgetParameterObject(tool));
#if RANDYTODO
				// TODO: See LexiconEditTool for how to set up all manner of menus and tool bars.
#endif
				CreateBrowseViewContextMenu();
			}

			private void CreateBrowseViewContextMenu()
			{
				// The actual menu declaration has a gazillion menu items, but only two of them are seen in this tool (plus the separator).
				// Start: <menu id="mnuBrowseView" (partial) >
				var contextMenuStrip = new ContextMenuStrip
				{
					Name = ContextMenuName.mnuBrowseView.ToString()
				};
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(3);
				// <command id="CmdPhonemeJumpToDefault" label="Show in Phonemes Editor" message="JumpToTool">
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, CmdPhonemeJumpToDefault_Clicked, AreaResources.Show_in_Phonemes_Editor);
				ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);
				// <command id="CmdDeleteSelectedObject" label="Delete selected {0}" message="DeleteSelectedItem"/>
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, CmdDeleteSelectedObject_Clicked, string.Format(AreaResources.Delete_selected_0, StringTable.Table.GetString("PhPhoneme", StringTable.ClassNames)));
				contextMenuStrip.Opening += ContextMenuStrip_Opening;

				// End: <menu id="mnuBrowseView" (partial) >
				_assignFeaturesToPhonemesView.ContextMenuStrip = contextMenuStrip;
			}

			private void ContextMenuStrip_Opening(object sender, System.ComponentModel.CancelEventArgs e)
			{
				_assignFeaturesToPhonemesView.ContextMenuStrip.Visible = !_recordList.HasEmptyList;
			}

			private void CmdDeleteSelectedObject_Clicked(object sender, EventArgs e)
			{
				_recordList.DeleteRecord(string.Format(AreaResources.Delete_selected_0, StringTable.Table.GetString("PhPhoneme", StringTable.ClassNames)), StatusBarPanelServices.GetStatusBarProgressPanel(_majorFlexComponentParameters.StatusBar));
			}

			private void CmdPhonemeJumpToDefault_Clicked(object sender, EventArgs e)
			{
				LinkHandler.PublishFollowLinkMessage(_majorFlexComponentParameters.FlexComponentParameters.Publisher, new FwLinkArgs(AreaServices.PhonemeEditMachineName, _recordList.CurrentObject.Guid));
			}

			#region Implementation of IDisposable
			private bool _isDisposed;

			~BulkEditPhonemesToolMenuHelper()
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
					if (_assignFeaturesToPhonemesView?.ContextMenuStrip != null)
					{
						_assignFeaturesToPhonemesView.ContextMenuStrip.Opening -= ContextMenuStrip_Opening;
						_assignFeaturesToPhonemesView.ContextMenuStrip.Dispose();
						_assignFeaturesToPhonemesView.ContextMenuStrip = null;
					}
				}
				_majorFlexComponentParameters = null;
				_assignFeaturesToPhonemesView = null;
				_recordList = null;

				_isDisposed = true;
			}
			#endregion
		}
	}
}