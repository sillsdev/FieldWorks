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
using LanguageExplorer.Controls.PaneBar;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.Resources;
using SIL.LCModel.Application;

namespace LanguageExplorer.Areas.Lexicon.Tools.ClassifiedDictionary
{
	/// <summary>
	/// ITool implementation for the "lexiconClassifiedDictionary" tool in the "lexicon" area.
	/// </summary>
	[Export(AreaServices.LexiconAreaMachineName, typeof(ITool))]
	internal sealed class LexiconClassifiedDictionaryTool : ITool
	{
		private LexiconClassifiedDictionaryToolMenuHelper _toolMenuHelper;
		private PaneBarContainer _paneBarContainer;
		private IRecordList _recordList;
		[Import(AreaServices.LexiconAreaMachineName)]
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
			PaneBarContainerFactory.RemoveFromParentAndDispose(majorFlexComponentParameters.MainCollapsingSplitContainer, ref _paneBarContainer);

			// Dispose after the main UI stuff.
			_toolMenuHelper.Dispose();

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
			if (_recordList == null)
			{
				_recordList = majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue<IRecordListRepositoryForTools>(LanguageExplorerConstants.RecordListRepository).GetRecordList(LexiconArea.SemanticDomainList_LexiconArea, majorFlexComponentParameters.StatusBar, LexiconArea.SemanticDomainList_LexiconAreaFactoryMethod);
			}
			_toolMenuHelper = new LexiconClassifiedDictionaryToolMenuHelper(majorFlexComponentParameters, this);
			var panelButton = new PanelButton(majorFlexComponentParameters.FlexComponentParameters, null, PaneBarContainerFactory.CreateShowFailingItemsPropertyName(MachineName), LexiconResources.Show_Unused_Items, LexiconResources.Show_Unused_Items)
			{
				Dock = DockStyle.Right
			};
			var xmlDocViewPaneBar = new PaneBar();
			xmlDocViewPaneBar.AddControls(new List<Control> { panelButton });
			// NB: XmlDocView adds user control handler.
			var xmlDocView = new XmlDocView(XDocument.Parse(LexiconResources.LexiconClassifiedDictionaryParameters).Root, majorFlexComponentParameters.LcmCache, _recordList, majorFlexComponentParameters.UiWidgetController);
			_paneBarContainer = PaneBarContainerFactory.Create(majorFlexComponentParameters.FlexComponentParameters, majorFlexComponentParameters.MainCollapsingSplitContainer, xmlDocView, xmlDocViewPaneBar);

			// Too early before now.
			_toolMenuHelper.DocView = xmlDocView;
			((ISemanticDomainTreeBarHandler)_recordList.MyTreeBarHandler).FinishInitialization(xmlDocViewPaneBar);
		}

		/// <summary>
		/// Do whatever might be needed to get ready for a refresh.
		/// </summary>
		public void PrepareToRefresh()
		{
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
		public string MachineName => AreaServices.LexiconClassifiedDictionaryMachineName;

		/// <summary>
		/// User-visible localizable component name.
		/// </summary>
		public string UiName => "Classified Dictionary";
		#endregion

		#region Implementation of ITool

		/// <summary>
		/// Get the area for the tool.
		/// </summary>
		public IArea Area => _area;

		/// <summary>
		/// Get the image for the area.
		/// </summary>
		public Image Icon => Images.DocumentView.SetBackgroundColor(Color.Magenta);

		#endregion

		private sealed class LexiconClassifiedDictionaryToolMenuHelper : IDisposable
		{
			private MajorFlexComponentParameters _majorFlexComponentParameters;
			internal XmlDocView DocView { get; set; }

			internal LexiconClassifiedDictionaryToolMenuHelper(MajorFlexComponentParameters majorFlexComponentParameters, ITool tool)
			{
				Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
				Guard.AgainstNull(tool, nameof(tool));

				_majorFlexComponentParameters = majorFlexComponentParameters;

				var toolUiWidgetParameterObject = new ToolUiWidgetParameterObject(tool);
				var editMenuDictionary = toolUiWidgetParameterObject.MenuItemsForTool[MainMenu.Edit];
				editMenuDictionary.Add(Command.CmdFindAndReplaceText, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(CmdFindAndReplaceText_Click, ()=> CanCmdFindAndReplaceText));
				editMenuDictionary.Add(Command.CmdReplaceText, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(CmdCmdReplaceText_Click, () => CanCmdCmdReplaceText));
				majorFlexComponentParameters.UiWidgetController.AddHandlers(toolUiWidgetParameterObject);
			}

			private static Tuple<bool, bool> CanCmdFindAndReplaceText => new Tuple<bool, bool>(true, true);

			private void CmdFindAndReplaceText_Click(object sender, EventArgs e)
			{
				_majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue<IApp>(LanguageExplorerConstants.App).ShowFindReplaceDialog(true, _majorFlexComponentParameters.MainWindow.ActiveView as IVwRootSite, _majorFlexComponentParameters.LcmCache, _majorFlexComponentParameters.MainWindow as Form);
			}

			private Tuple<bool, bool> CanCmdCmdReplaceText => new Tuple<bool, bool>(true, DocView.CanUseReplaceText());

			private void CmdCmdReplaceText_Click(object sender, EventArgs e)
			{
				_majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue<IApp>(LanguageExplorerConstants.App).ShowFindReplaceDialog(false, _majorFlexComponentParameters.MainWindow.ActiveView as IVwRootSite, _majorFlexComponentParameters.LcmCache, _majorFlexComponentParameters.MainWindow as Form);
			}

			#region Implementation of IDisposable
			private bool _isDisposed;

			~LexiconClassifiedDictionaryToolMenuHelper()
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
				}
				_majorFlexComponentParameters = null;
				DocView = null;

				_isDisposed = true;
			}
			#endregion
		}
	}
}