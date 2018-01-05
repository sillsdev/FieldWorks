// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.DetailControls;
using LanguageExplorer.Controls.PaneBar;
using SIL.FieldWorks.Resources;
using SIL.LCModel.Application;
using SIL.LCModel.Utils;

namespace LanguageExplorer.Areas.TextsAndWords.Tools.Analyses
{
	/// <summary>
	/// ITool implementation for the "Analyses" tool in the "textsWords" area.
	/// </summary>
	[Export(AreaServices.TextAndWordsAreaMachineName, typeof(ITool))]
	internal sealed class AnalysesTool : ITool
	{
		private AreaWideMenuHelper _areaWideMenuHelper;
		private TextAndWordsAreaMenuHelper _textAndWordsAreaMenuHelper;
		private MultiPane _multiPane;
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
			_areaWideMenuHelper.Dispose();
			_textAndWordsAreaMenuHelper.Dispose();
			MultiPaneFactory.RemoveFromParentAndDispose(majorFlexComponentParameters.MainCollapsingSplitContainer, ref _multiPane);
			_recordBrowseView = null;
			_areaWideMenuHelper = null;
			_textAndWordsAreaMenuHelper = null;
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
				_recordList = majorFlexComponentParameters.RecordListRepositoryForTools.GetRecordList(TextAndWordsArea.ConcordanceWords, majorFlexComponentParameters.Statusbar, TextAndWordsArea.ConcordanceWordsFactoryMethod);
			}
			_areaWideMenuHelper = new AreaWideMenuHelper(majorFlexComponentParameters, _recordList);
			_areaWideMenuHelper.SetupFileExportMenu();
			_textAndWordsAreaMenuHelper = new TextAndWordsAreaMenuHelper(majorFlexComponentParameters);
			_textAndWordsAreaMenuHelper.AddMenusForAllButConcordanceTool();

			var root = XDocument.Parse(TextAndWordsResources.WordListParameters).Root;
			var columnsElement = XElement.Parse(TextAndWordsResources.WordListColumns);
			var overriddenColumnElement = columnsElement.Elements("column").First(column => column.Attribute("label").Value == "Form");
			overriddenColumnElement.Attribute("width").Value = "25%";
			overriddenColumnElement = columnsElement.Elements("column").First(column => column.Attribute("label").Value == "Word Glosses");
			overriddenColumnElement.Attribute("width").Value = "25%";
			// LT-8373.The point of these overrides: By default, enable User Analyses for "Word Analyses"
			overriddenColumnElement = columnsElement.Elements("column").First(column => column.Attribute("label").Value == "User Analyses");
			overriddenColumnElement.Attribute("visibility").Value = "always";
			overriddenColumnElement.Add(new XAttribute("width", "15%"));
			root.Add(columnsElement);
			_recordBrowseView = new RecordBrowseView(root, majorFlexComponentParameters.LcmCache, _recordList);
#if RANDYTODO
			// TODO: See LexiconEditTool for how to set up all manner of menus and toolbars.
#endif
			var dataTree = new DataTree();
			var recordEditView = new RecordEditView(XElement.Parse(TextAndWordsResources.AnalysesRecordEditViewParameters), XDocument.Parse(AreaResources.VisibilityFilter_All), majorFlexComponentParameters.LcmCache, _recordList, dataTree, MenuServices.GetFilePrintMenu(majorFlexComponentParameters.MenuStrip));
			_multiPane = MultiPaneFactory.CreateMultiPaneWithTwoPaneBarContainersInMainCollapsingSplitContainer(
				majorFlexComponentParameters.FlexComponentParameters,
				majorFlexComponentParameters.MainCollapsingSplitContainer,
				new MultiPaneParameters
				{
					Orientation = Orientation.Vertical,
					Area = _area,
					Id = "WordsAndAnalysesMultiPane",
					ToolMachineName = MachineName
				},
				_recordBrowseView, "WordList", new PaneBar(),
				recordEditView, "SingleWord", new PaneBar());

			using (var gr = _multiPane.CreateGraphics())
			{
				_multiPane.Panel2MinSize = Math.Max((int)(180000 * gr.DpiX) / MiscUtils.kdzmpInch,
						CollapsingSplitContainer.kCollapseZone);
			}

			// Too early before now.
			recordEditView.FinishInitialization();
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
		public string MachineName => AreaServices.AnalysesMachineName;

		/// <summary>
		/// User-visible localizable component name.
		/// </summary>
		public string UiName => "Word Analyses";
		#endregion

		#region Implementation of ITool

		/// <summary>
		/// Get the area for the tool.
		/// </summary>
		public IArea Area => _area;

		/// <summary>
		/// Get the image for the area.
		/// </summary>
		public Image Icon => Images.SideBySideView.SetBackgroundColor(Color.Magenta);

		#endregion
	}
}
