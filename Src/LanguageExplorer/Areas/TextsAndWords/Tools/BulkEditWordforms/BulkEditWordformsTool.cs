// Copyright (c) 2015-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.ComponentModel.Composition;
using System.Drawing;
using System.Linq;
using System.Xml.Linq;
using LanguageExplorer.Controls;
using SIL.FieldWorks.Resources;
using SIL.LCModel.Application;

namespace LanguageExplorer.Areas.TextsAndWords.Tools.BulkEditWordforms
{
	/// <summary>
	/// ITool implementation for the "bulkEditWordforms" tool in the "textsWords" area.
	/// </summary>
	[Export(AreaServices.TextAndWordsAreaMachineName, typeof(ITool))]
	internal sealed class BulkEditWordformsTool : ITool
	{
		private PartiallySharedAreaWideMenuHelper _partiallySharedAreaWideMenuHelper;
		private PartiallySharedTextsAndWordsToolsMenuHelper _partiallySharedTextsAndWordsToolsMenuHelper;
		private BrowseViewContextMenuFactory _browseViewContextMenuFactory;
		private PaneBarContainer _paneBarContainer;
		private RecordBrowseView _recordBrowseView;
		private IRecordList _recordList;
		[Import(AreaServices.TextAndWordsAreaMachineName)]
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
			// This will also remove any event handlers set up by any of the tool's UserControl instances that may have registered event handlers.
			majorFlexComponentParameters.UiWidgetController.RemoveToolHandlers();
			PaneBarContainerFactory.RemoveFromParentAndDispose(majorFlexComponentParameters.MainCollapsingSplitContainer, ref _paneBarContainer);

			// Dispose after the main UI stuff.
			_browseViewContextMenuFactory.Dispose();
			_partiallySharedAreaWideMenuHelper.Dispose();

			_recordBrowseView = null;
			_partiallySharedAreaWideMenuHelper = null;
			_browseViewContextMenuFactory = null;
			_partiallySharedTextsAndWordsToolsMenuHelper = null;
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
				_recordList = majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue<IRecordListRepositoryForTools>(LanguageExplorerConstants.RecordListRepository).GetRecordList(TextAndWordsArea.ConcordanceWords, majorFlexComponentParameters.StatusBar, TextAndWordsArea.ConcordanceWordsFactoryMethod);
			}
			var toolUiWidgetParameterObject = new ToolUiWidgetParameterObject(this);
			_partiallySharedAreaWideMenuHelper = new PartiallySharedAreaWideMenuHelper(majorFlexComponentParameters, _recordList);
			_partiallySharedAreaWideMenuHelper.SetupFileExportMenu(toolUiWidgetParameterObject);
			_partiallySharedTextsAndWordsToolsMenuHelper = new PartiallySharedTextsAndWordsToolsMenuHelper(majorFlexComponentParameters);
			_partiallySharedTextsAndWordsToolsMenuHelper.AddMenusForExpectedTextAndWordsTools(toolUiWidgetParameterObject);
			majorFlexComponentParameters.UiWidgetController.AddHandlers(toolUiWidgetParameterObject);
			_browseViewContextMenuFactory = new BrowseViewContextMenuFactory();
#if RANDYTODO
			// TODO: Set up factory method for the browse view.
#endif

			var root = XDocument.Parse(TextAndWordsResources.BulkEditWordformsToolParameters).Root;
			root.Element("includeColumns").ReplaceWith(XElement.Parse(TextAndWordsResources.WordListColumns));
			var columns = root.Element("columns");
			var currentColumn = columns.Elements("column").First(col => col.Attribute("label").Value == "Form");
			currentColumn.Attribute("width").Value = "80000";
			currentColumn.Attribute("ws").Value = "$ws=vernacular";
			currentColumn.Attribute("cansortbylength").Value = "true";
			currentColumn.Add(new XAttribute("transduce", "WfiWordform.Form"));
			currentColumn.Add(new XAttribute("editif", "!FormIsUsedWithWs"));
			currentColumn.Element("span").Element("string").Attribute("ws").Value = "$ws=vernacular";
			currentColumn = columns.Elements("column").First(col => col.Attribute("label").Value == "Word Glosses");
			currentColumn.Attribute("width").Value = "80000";
			currentColumn = columns.Elements("column").First(col => col.Attribute("label").Value == "Spelling Status");
			currentColumn.Add(new XAttribute("width", "65000"));
			_recordBrowseView = new RecordBrowseView(root, _browseViewContextMenuFactory, majorFlexComponentParameters.LcmCache, _recordList, majorFlexComponentParameters.UiWidgetController);

			_paneBarContainer = PaneBarContainerFactory.Create(majorFlexComponentParameters.FlexComponentParameters, majorFlexComponentParameters.MainCollapsingSplitContainer, _recordBrowseView);

		}

		/// <summary>
		/// Do whatever might be needed to get ready for a refresh.
		/// </summary>
		public void PrepareToRefresh()
		{
			_recordBrowseView.BrowseViewer.BrowseView.PrepareToRefresh();
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
		public string MachineName => AreaServices.BulkEditWordformsMachineName;

		/// <summary>
		/// User-visible localizable component name.
		/// </summary>
		public string UiName => "Bulk Edit Wordforms";
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
	}
}