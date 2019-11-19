// Copyright (c) 2015-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Areas.TextsAndWords.Interlinear;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.DetailControls;
using LanguageExplorer.Controls.PaneBar;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Utils;

namespace LanguageExplorer.Areas.TextsAndWords.Tools.Analyses
{
	/// <summary>
	/// ITool implementation for the "Analyses" tool in the "textsWords" area.
	/// </summary>
	[Export(AreaServices.TextAndWordsAreaMachineName, typeof(ITool))]
	internal sealed class AnalysesTool : ITool
	{
		private AnalysesToolMenuHelper _toolMenuHelper;
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
			// This will also remove any event handlers set up by any of the tool's UserControl instances that may have registered event handlers.
			majorFlexComponentParameters.UiWidgetController.RemoveToolHandlers();
			MultiPaneFactory.RemoveFromParentAndDispose(majorFlexComponentParameters.MainCollapsingSplitContainer, ref _multiPane);

			// Dispose after the main UI stuff.
			_toolMenuHelper.Dispose();

			_recordBrowseView = null;
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
				_recordList = majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue<IRecordListRepositoryForTools>(LanguageExplorerConstants.RecordListRepository).GetRecordList(TextAndWordsArea.ConcordanceWords, majorFlexComponentParameters.StatusBar, TextAndWordsArea.ConcordanceWordsFactoryMethod);
			}
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
			_recordBrowseView = new RecordBrowseView(root, majorFlexComponentParameters.LcmCache, _recordList, majorFlexComponentParameters.UiWidgetController);
			var dataTree = new DataTree(majorFlexComponentParameters.SharedEventHandlers, majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue(UiWidgetServices.CreateShowHiddenFieldsPropertyName(MachineName), false));
			var recordEditView = new RecordEditView(XElement.Parse(TextAndWordsResources.AnalysesRecordEditViewParameters), XDocument.Parse(AreaResources.VisibilityFilter_All), majorFlexComponentParameters.LcmCache, _recordList, dataTree, majorFlexComponentParameters.UiWidgetController);
			_toolMenuHelper = new AnalysesToolMenuHelper(majorFlexComponentParameters, this, _recordBrowseView, _recordList, dataTree);
			_multiPane = MultiPaneFactory.CreateMultiPaneWithTwoPaneBarContainersInMainCollapsingSplitContainer(majorFlexComponentParameters.FlexComponentParameters, majorFlexComponentParameters.MainCollapsingSplitContainer,
				new MultiPaneParameters
				{
					Orientation = Orientation.Vertical,
					Area = _area,
					Id = "WordsAndAnalysesMultiPane",
					ToolMachineName = MachineName
				}, _recordBrowseView, "WordList", new PaneBar(), recordEditView, "SingleWord", new PaneBar());
			using (var gr = _multiPane.CreateGraphics())
			{
				_multiPane.Panel2MinSize = Math.Max((int)(180000 * gr.DpiX) / MiscUtils.kdzmpInch, CollapsingSplitContainer.kCollapseZone);
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
		/// User-visible localized component name.
		/// </summary>
		public string UiName => StringTable.Table.LocalizeLiteralValue(AreaServices.AnalysesUiName);

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

		private sealed class AnalysesToolMenuHelper : IDisposable
		{
			private MajorFlexComponentParameters _majorFlexComponentParameters;
			private FileExportMenuHelper _fileExportMenuHelper;
			private PartiallySharedForToolsWideMenuHelper _partiallySharedForToolsWideMenuHelper;
			private PartiallySharedTextsAndWordsToolsMenuHelper _partiallySharedTextsAndWordsToolsMenuHelper;
			private RecordBrowseView _recordBrowseView;
			private IRecordList _recordList;
			private DataTree _dataTree;
			private ToolStripMenuItem _wordformJumpToConcordanceMenu;
			private ToolStripMenuItem _analysisJumpToConcordanceMenu;
			private IPropertyTable _propertyTable;
			private LcmCache _cache;
			private ISharedEventHandlers _sharedEventHandlers;
			private const string UserOpinion = "UserOpinion";

			/// <summary>
			/// Returns the object of the current slice, or (if no slice is marked current)
			/// the object of the first slice, or (if there are no slices, or no data entry form) null.
			/// </summary>
			private ICmObject CurrentSliceObject => _dataTree.CurrentSlice != null ? _dataTree.CurrentSlice.MyCmObject : !_dataTree.Slices.Any() ? null : _dataTree.FieldAt(0).MyCmObject;

			private IWfiWordform Wordform
			{
				get
				{
					// Note that we may get here after the owner (or the owner's owner) of the
					// current object has been deleted: see LT-10124.
					var curObject = CurrentSliceObject;
					if (curObject is IWfiWordform)
					{
						return (IWfiWordform)curObject;
					}

					if (curObject is IWfiAnalysis && curObject.Owner != null)
					{
						return (IWfiWordform)(curObject.Owner);
					}
					if (curObject is IWfiGloss && curObject.Owner != null)
					{
						var anal = curObject.OwnerOfClass<IWfiAnalysis>();
						if (anal.Owner != null)
						{
							return anal.OwnerOfClass<IWfiWordform>();
						}
					}
					return null;
				}
			}

			private IWfiAnalysis Analysis
			{
				get
				{
					var curObject = CurrentSliceObject;
					return curObject is IWfiAnalysis ? (IWfiAnalysis)curObject : curObject is IWfiGloss ? curObject.OwnerOfClass<IWfiAnalysis>() : null;
				}
			}

			private IWfiGloss Gloss
			{
				get
				{
					var curObject = CurrentSliceObject;
					return curObject is IWfiGloss ? (IWfiGloss)curObject : null;
				}
			}

			internal AnalysesToolMenuHelper(MajorFlexComponentParameters majorFlexComponentParameters, ITool tool, RecordBrowseView recordBrowseView, IRecordList recordList, DataTree dataTree)
			{
				Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
				Guard.AgainstNull(tool, nameof(tool));
				Guard.AgainstNull(recordBrowseView, nameof(recordBrowseView));
				Guard.AgainstNull(recordList, nameof(recordList));
				Guard.AgainstNull(dataTree, nameof(dataTree));

				_majorFlexComponentParameters = majorFlexComponentParameters;
				_recordBrowseView = recordBrowseView;
				_recordList = recordList;
				_dataTree = dataTree;
				_propertyTable = _majorFlexComponentParameters.FlexComponentParameters.PropertyTable;
				_cache = _majorFlexComponentParameters.LcmCache;
				_sharedEventHandlers = _majorFlexComponentParameters.SharedEventHandlers;

				SetupUiWidgets(tool);
				CreateBrowseViewContextMenu();
			}

			private void SetupUiWidgets(ITool tool)
			{
				_partiallySharedForToolsWideMenuHelper = new PartiallySharedForToolsWideMenuHelper(_majorFlexComponentParameters, _recordList);
				var toolUiWidgetParameterObject = new ToolUiWidgetParameterObject(tool);
				_fileExportMenuHelper = new FileExportMenuHelper(_majorFlexComponentParameters);
				_fileExportMenuHelper.SetupFileExportMenu(toolUiWidgetParameterObject);
				_partiallySharedTextsAndWordsToolsMenuHelper = new PartiallySharedTextsAndWordsToolsMenuHelper(_majorFlexComponentParameters);
				_partiallySharedTextsAndWordsToolsMenuHelper.AddMenusForExpectedTextAndWordsTools(toolUiWidgetParameterObject);
				var insertMenuDictionary = toolUiWidgetParameterObject.MenuItemsForTool[MainMenu.Insert];
				var insertToolBarDictionary = toolUiWidgetParameterObject.ToolBarItemsForTool[ToolBar.Insert];
				AreaServices.InsertPair(insertToolBarDictionary, insertMenuDictionary,
					Command.CmdInsertHumanApprovedAnalysis, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(InsertHumanApprovedAnalysis_Click, () => UiWidgetServices.CanSeeAndDo));

				_majorFlexComponentParameters.UiWidgetController.AddHandlers(toolUiWidgetParameterObject);

				RegisterSliceLeftEdgeMenus();
			}

			private void RegisterSliceLeftEdgeMenus()
			{
				#region Left edge context menus

				// <menu id="mnuDataTree_MainWordform">
				_dataTree.DataTreeSliceContextMenuParameterObject.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ContextMenuName.mnuDataTree_MainWordform, Create_mnuDataTree_MainWordform);

				// <menu id="mnuDataTree_HumanApprovedAnalysisSummary">
				_dataTree.DataTreeSliceContextMenuParameterObject.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ContextMenuName.mnuDataTree_HumanApprovedAnalysisSummary, Create_mnuDataTree_HumanApprovedAnalysisSummary);

				// <menu id="mnuDataTree_HumanApprovedAnalysis">
				_dataTree.DataTreeSliceContextMenuParameterObject.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ContextMenuName.mnuDataTree_HumanApprovedAnalysis, Create_mnuDataTree_HumanApprovedAnalysis);

				// <menu id="mnuDataTree_WordGlossForm">
				_dataTree.DataTreeSliceContextMenuParameterObject.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ContextMenuName.mnuDataTree_WordGlossForm, Create_mnuDataTree_WordGlossForm);

				// <menu id="mnuDataTree_WordformSpelling">
				_dataTree.DataTreeSliceContextMenuParameterObject.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ContextMenuName.mnuDataTree_WordformSpelling, Create_mnuDataTree_WordformSpelling);

				// <menu id="mnuDataTree_ParserProducedAnalysis">
				_dataTree.DataTreeSliceContextMenuParameterObject.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ContextMenuName.mnuDataTree_ParserProducedAnalysis, Create_mnuDataTree_ParserProducedAnalysis);

				// <menu id="mnuDataTree_HumanDisapprovedAnalysis">
				_dataTree.DataTreeSliceContextMenuParameterObject.LeftEdgeContextMenuFactory.RegisterLeftEdgeContextMenuCreatorMethod(ContextMenuName.mnuDataTree_HumanDisapprovedAnalysis, Create_mnuDataTree_HumanDisapprovedAnalysis);

				#endregion Left edge context menus

				#region Hotlinks menus

				// mnuDataTree_MainWordform_Hotlinks
				_dataTree.DataTreeSliceContextMenuParameterObject.HotlinksMenuFactory.RegisterHotlinksMenuCreatorMethod(ContextMenuName.mnuDataTree_MainWordform_Hotlinks, Create_mnuDataTree_MainWordform_Hotlinks);

				// mnuDataTree_HumanApprovedAnalysisSummary_Hotlinks
				_dataTree.DataTreeSliceContextMenuParameterObject.HotlinksMenuFactory.RegisterHotlinksMenuCreatorMethod(ContextMenuName.mnuDataTree_HumanApprovedAnalysisSummary_Hotlinks, Create_mnuDataTree_HumanApprovedAnalysisSummary_Hotlinks);

				// <menu id="mnuDataTree_HumanApprovedAnalysis_Hotlinks">
				_dataTree.DataTreeSliceContextMenuParameterObject.HotlinksMenuFactory.RegisterHotlinksMenuCreatorMethod(ContextMenuName.mnuDataTree_HumanApprovedAnalysis_Hotlinks, Create_mnuDataTree_HumanApprovedAnalysis_Hotlinks);

				#endregion Hotlinks menus
			}

			private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_ParserProducedAnalysis(Slice slice, ContextMenuName contextMenuId)
			{
				Require.That(contextMenuId == ContextMenuName.mnuDataTree_ParserProducedAnalysis, $"Expected argument value of '{ContextMenuName.mnuDataTree_ParserProducedAnalysis.ToString()}', but got '{contextMenuId.ToString()}' instead.");

				// Start: <menu id="mnuDataTree_ParserProducedAnalysis">

				var contextMenuStrip = new ContextMenuStrip
				{
					Name = ContextMenuName.mnuDataTree_ParserProducedAnalysis.ToString()
				};
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(6);

				// <menu id="mnuDataTree_ParserProducedStatus" label="User Opinion">
				Create_User_Opinion_Menu(contextMenuStrip);
				// <item label="-" translate="do not translate" />
				ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);
				// <item command="CmdDataTree_Delete_ParserProducedAnalysis" />
				AreaServices.CreateDeleteMenuItem(menuItems, contextMenuStrip, slice, TextAndWordsResources.Delete_Candidate, _sharedEventHandlers.Get(AreaServices.DataTreeDelete));

				contextMenuStrip.Opening += ContextMenuStripOnOpening;

				// End: <menu id="mnuDataTree_ParserProducedAnalysis">

				return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
			}

			private void ContextMenuStripOnOpening(object sender, CancelEventArgs e)
			{
				var contextMenuStrip = (ContextMenuStrip)sender;
				foreach (ToolStripDropDownItem item in contextMenuStrip.Items)
				{
					if (item.Name != UserOpinion)
					{
						continue;
					}
					var analysis = Analysis;
					((ToolStripMenuItem)item.DropDownItems[0]).Checked = analysis.ApprovalStatusIcon == 1;
					((ToolStripMenuItem)item.DropDownItems[1]).Checked = analysis.ApprovalStatusIcon == 0;
					((ToolStripMenuItem)item.DropDownItems[2]).Checked = analysis.ApprovalStatusIcon == 2;
					break;
				}
			}

			private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_HumanDisapprovedAnalysis(Slice slice, ContextMenuName contextMenuId)
			{
				Require.That(contextMenuId == ContextMenuName.mnuDataTree_HumanDisapprovedAnalysis, $"Expected argument value of '{ContextMenuName.mnuDataTree_HumanDisapprovedAnalysis.ToString()}', but got '{contextMenuId.ToString()}' instead.");

				// Start: <menu id="mnuDataTree_HumanDisapprovedAnalysis">

				var contextMenuStrip = new ContextMenuStrip
				{
					Name = ContextMenuName.mnuDataTree_HumanDisapprovedAnalysis.ToString()
				};
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(6);

				// <menu id="mnuDataTree_HumanDisapprovedStatus" label="User Opinion">
				Create_User_Opinion_Menu(contextMenuStrip);
				// <item label="-" translate="do not translate" />
				ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);
				// <item command="CmdDataTree_Delete_HumanDisapprovedAnalysis" />
				AreaServices.CreateDeleteMenuItem(menuItems, contextMenuStrip, slice, TextAndWordsResources.Delete_Disapproved_Analysis, _sharedEventHandlers.Get(AreaServices.DataTreeDelete));

				contextMenuStrip.Opening += ContextMenuStripOnOpening;

				// End: <menu id="mnuDataTree_HumanDisapprovedAnalysis">

				return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
			}

			private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_HumanApprovedAnalysis(Slice slice, ContextMenuName contextMenuId)
			{
				Require.That(contextMenuId == ContextMenuName.mnuDataTree_HumanApprovedAnalysis, $"Expected argument value of '{ContextMenuName.mnuDataTree_HumanApprovedAnalysis.ToString()}', but got '{contextMenuId.ToString()}' instead.");

				// Start: <menu id="mnuDataTree_HumanApprovedAnalysis">

				var contextMenuStrip = new ContextMenuStrip
				{
					Name = ContextMenuName.mnuDataTree_HumanApprovedAnalysis.ToString()
				};
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(9);

				// <item command="CmdShowHumanApprovedAnalysisConc" />
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, ShowHumanApprovedAnalysisConc_Click, TextAndWordsResources.Assign_Analysis);
				// <item command="CmdAnalysisJumpToConcordance" />
				_analysisJumpToConcordanceMenu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _majorFlexComponentParameters.SharedEventHandlers.Get(AreaServices.JumpToTool), AreaResources.Show_Analysis_in_Concordance);
				_analysisJumpToConcordanceMenu.Tag = new List<object> { _majorFlexComponentParameters.FlexComponentParameters.Publisher, AreaServices.ConcordanceMachineName, _dataTree };
				Create_User_Opinion_Menu(contextMenuStrip);
				// <item command="CmdDataTree_Insert_WordGloss" />
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Insert_WordGloss_Click, TextAndWordsResources.Add_Word_Gloss);
				// <item label="-" translate="do not translate" />
				ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);
				// <item command="CmdDataTree_Delete_HumanApprovedAnalysis" />
				AreaServices.CreateDeleteMenuItem(menuItems, contextMenuStrip, slice, TextAndWordsResources.Delete_Analysis, _sharedEventHandlers.Get(AreaServices.DataTreeDelete));

				contextMenuStrip.Opening += ContextMenuStripOnOpening;

				// End: <menu id="mnuDataTree_HumanApprovedAnalysis">

				return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
			}

			private void Create_User_Opinion_Menu(ContextMenuStrip contextMenuStrip)
			{
				// <menu id="mainMenuName" label="User Opinion">
				var owningMenuItem = ToolStripMenuItemFactory.CreateBaseMenuForToolStripMenuItem(contextMenuStrip, TextAndWordsResources.User_Opinion);
				owningMenuItem.Name = UserOpinion;
				// <item command="CmdAnalysisApprove" />
				ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(owningMenuItem, AnalysisApprove_Clicked, TextAndWordsResources.Approve);
				// <item command="CmdAnalysisUnknown" />
				ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(owningMenuItem, AnalysisUnknown_Clicked, TextAndWordsResources.Unknown);
				// <item command="CmdAnalysisDisapprove" />
				ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(owningMenuItem, AnalysisDisapprove_Clicked, TextAndWordsResources.Disapprove);
			}

			private void Insert_WordGloss_Click(object sender, EventArgs e)
			{
				UowHelpers.UndoExtension(TextAndWordsResources.Insert_Word_Gloss, _cache.ActionHandlerAccessor, () =>
				{
					Analysis.MeaningsOC.Add(_cache.ServiceLocator.GetInstance<IWfiGlossFactory>().Create());
				});
			}

			private void AnalysisApprove_Clicked(object sender, EventArgs e)
			{
				SetNewStatus(Analysis, 1);
			}

			private void AnalysisUnknown_Clicked(object sender, EventArgs e)
			{
				SetNewStatus(Analysis, 0);
			}

			private void AnalysisDisapprove_Clicked(object sender, EventArgs e)
			{
				SetNewStatus(Analysis, 2);
			}

			private void SetNewStatus(IWfiAnalysis anal, int newStatus)
			{
				var currentStatus = anal.ApprovalStatusIcon;
				if (currentStatus == newStatus)
				{
					return;
				}
				UowHelpers.UndoExtension(TextAndWordsResources.Changing_Approval_Status, _cache.ActionHandlerAccessor, () =>
				{
					if (currentStatus == 1)
					{
						anal.MoveConcAnnotationsToWordform();
					}
					anal.ApprovalStatusIcon = newStatus;
					if (newStatus == 1)
					{
						// make sure default senses are set to be real values,
						// since the user has seen the defaults, and approved the analysis based on them.
						foreach (var mb in anal.MorphBundlesOS)
						{
							var currentSense = mb.SenseRA;
							if (currentSense == null)
								mb.SenseRA = mb.DefaultSense;
						}
					}
				});
				// Wipe all of the old slices out, so we get new numbers and newly placed objects.
				// This fixes LT-5935. Also removes the need to somehow make the virtual properties like HumanApprovedAnalyses update.
				_dataTree.RefreshList(true);
			}

			private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_MainWordform(Slice slice, ContextMenuName contextMenuId)
			{
				Require.That(contextMenuId == ContextMenuName.mnuDataTree_MainWordform, $"Expected argument value of '{ContextMenuName.mnuDataTree_MainWordform.ToString()}', but got '{contextMenuId.ToString()}' instead.");

				// Start: <menu id="mnuDataTree_MainWordform">

				var contextMenuStrip = new ContextMenuStrip
				{
					Name = ContextMenuName.mnuDataTree_MainWordform.ToString()
				};
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(3);
				// <item command="CmdShowWordformConc" />
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, ShowWordformConc_Click, TextAndWordsResources.Assign_Analysis);
				// <item command="CmdRespeller" />
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Respeller_Click, TextAndWordsResources.Change_Spelling);
				// <item command="CmdDataTree_Delete_MainWordform" />
				AreaServices.CreateDeleteMenuItem(menuItems, contextMenuStrip, slice, TextAndWordsResources.Delete_Wordform, _sharedEventHandlers.Get(AreaServices.DataTreeDelete));

				// End: <menu id="mnuDataTree_MainWordform">

				return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
			}

			private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_HumanApprovedAnalysisSummary(Slice slice, ContextMenuName contextMenuId)
			{
				Require.That(contextMenuId == ContextMenuName.mnuDataTree_HumanApprovedAnalysisSummary, $"Expected argument value of '{ContextMenuName.mnuDataTree_HumanApprovedAnalysisSummary.ToString()}', but got '{contextMenuId.ToString()}' instead.");

				// Start: <menu id="mnuDataTree_HumanApprovedAnalysisSummary">

				var contextMenuStrip = new ContextMenuStrip
				{
					Name = ContextMenuName.mnuDataTree_HumanApprovedAnalysisSummary.ToString()
				};
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);

				// <item command="CmdInsertHumanApprovedAnalysis" />
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, InsertHumanApprovedAnalysis_Click, TextAndWordsResources.Add_Approved_Analysis);

				// End: <menu id="mnuDataTree_HumanApprovedAnalysisSummary">

				return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
			}

			private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_WordformSpelling(Slice slice, ContextMenuName contextMenuId)
			{
				Require.That(contextMenuId == ContextMenuName.mnuDataTree_WordformSpelling, $"Expected argument value of '{ContextMenuName.mnuDataTree_WordformSpelling.ToString()}', but got '{contextMenuId.ToString()}' instead.");

				// Start: <menu id="mnuDataTree_WordformSpelling">

				var contextMenuStrip = new ContextMenuStrip
				{
					Name = ContextMenuName.mnuDataTree_WordformSpelling.ToString()
				};
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);

				// <item command="CmdRespeller" />
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Respeller_Click, TextAndWordsResources.Change_Spelling);

				// End: <menu id="mnuDataTree_WordformSpelling">

				return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
			}

			private void Respeller_Click(object sender, EventArgs e)
			{
				using (var luh = new ListUpdateHelper(new ListUpdateHelperParameterObject { MyRecordList = _recordList }))
				{
					var changesWereMade = false;
					using (var dlg = new RespellerDlg())
					{
						dlg.InitializeFlexComponent(_majorFlexComponentParameters.FlexComponentParameters);
						if (dlg.SetDlgInfo(_majorFlexComponentParameters.StatusBar))
						{
							dlg.ShowDialog((Form)_majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue<IFwMainWnd>(FwUtils.window));
							changesWereMade = dlg.ChangesWereMade;
						}
						else
						{
							MessageBox.Show(TextAndWordsResources.ksCannotRespellWordform);
						}
					}
					// The Respeller dialog can't make all necessary updates, since things like occurrence
					// counts depend on which texts are included, not just the data. So make sure we reload.
					luh.TriggerPendingReloadOnDispose = changesWereMade;
					if (changesWereMade)
					{
						// further try to refresh occurrence counts.
						var sda = _recordList.VirtualListPublisher;
						while (sda != null)
						{
							if (sda is ConcDecorator)
							{
								((ConcDecorator)sda).Refresh();
								break;
							}
							if (!(sda is DomainDataByFlidDecoratorBase))
							{
								break;
							}
							sda = ((DomainDataByFlidDecoratorBase)sda).BaseSda;
						}
					}
				}
			}

			private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> Create_mnuDataTree_WordGlossForm(Slice slice, ContextMenuName contextMenuId)
			{
				Require.That(contextMenuId == ContextMenuName.mnuDataTree_WordGlossForm, $"Expected argument value of '{ContextMenuName.mnuDataTree_WordGlossForm.ToString()}', but got '{contextMenuId.ToString()}' instead.");

				// Start: <menu id="mnuDataTree_WordGlossForm">

				var contextMenuStrip = new ContextMenuStrip
				{
					Name = ContextMenuName.mnuDataTree_WordGlossForm.ToString()
				};
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(5);

				// <item command="CmdShowWordGlossConc" />
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, ShowWordGlossConc_Click, TextAndWordsResources.Assign_Analysis);
				// <item command="CmdWordGlossJumpToConcordance" />
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, WordGlossJumpToConcordance_Click, TextAndWordsResources.Show_Word_Gloss_in_Concordance);
				// <item label="-" translate="do not translate" />
				ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);
				// <item command="CmdDataTree_Merge_WordGloss" />
				var enabled = slice.CanMergeNow;
				var menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, Merge_WordGloss_Click, enabled ? TextAndWordsResources.Merge_Gloss : $"{TextAndWordsResources.Merge_Gloss} {StringTable.Table.GetString("(cannot merge this)")}");
				menu.Enabled = enabled;
				// <item command="CmdDataTree_Delete_WordGloss" />
				AreaServices.CreateDeleteMenuItem(menuItems, contextMenuStrip, slice, TextAndWordsResources.Delete_Gloss, Delete_WordGloss_Clicked);

				// End: <menu id="mnuDataTree_WordGlossForm">

				return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
			}

			private void Delete_WordGloss_Clicked(object sender, EventArgs e)
			{
				_dataTree.CurrentSlice.HandleDeleteCommand();
			}

			private void Merge_WordGloss_Click(object sender, EventArgs e)
			{
				/*
    <command id="CmdDataTree_Merge_WordGloss" label="Merge Gloss..." message="DataTreeMerge">
      <parameters field="Meanings" className="WfiGloss" />
    </command>
				*/
				var currentSlice = _dataTree.CurrentSlice;
				currentSlice.HandleMergeCommand(true);
			}

			private void WordGlossJumpToConcordance_Click(object sender, EventArgs e)
			{
				/*
    <command id="CmdWordGlossJumpToConcordance" label="Show Word Gloss in Concordance" message="JumpToTool">
      <parameters tool="concordance" className="WfiGloss" />
    </command>
				*/
				LinkHandler.PublishFollowLinkMessage(_majorFlexComponentParameters.FlexComponentParameters.Publisher, new FwLinkArgs("concordance", _dataTree.CurrentSlice.MyCmObject.Guid));
			}

			private void ShowWordGlossConc_Click(object sender, EventArgs e)
			{
				ShowConcDlg(Gloss);
			}

			private List<Tuple<ToolStripMenuItem, EventHandler>> Create_mnuDataTree_MainWordform_Hotlinks(Slice slice, ContextMenuName hotlinksMenuId)
			{
				Require.That(hotlinksMenuId == ContextMenuName.mnuDataTree_MainWordform_Hotlinks, $"Expected argument value of '{ContextMenuName.mnuDataTree_MainWordform_Hotlinks.ToString()}', but got '{hotlinksMenuId.ToString()}' instead.");

				var hotlinksMenuItemList = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);

				/* <command id="CmdShowWordformConc" label="Assign Analysis..." message="ShowWordformConc" />
				<menu id="mnuDataTree_MainWordform_Hotlinks">
					<item command="CmdShowWordformConc" />
				</menu>
				*/
				ToolStripMenuItemFactory.CreateHotLinkToolStripMenuItem(hotlinksMenuItemList, ShowWordformConc_Click, TextAndWordsResources.Assign_Analysis);

				return hotlinksMenuItemList;
			}

			private void ShowWordformConc_Click(object sender, EventArgs e)
			{
				ShowConcDlg(Wordform);
			}

			private List<Tuple<ToolStripMenuItem, EventHandler>> Create_mnuDataTree_HumanApprovedAnalysisSummary_Hotlinks(Slice slice, ContextMenuName hotlinksMenuId)
			{
				Require.That(hotlinksMenuId == ContextMenuName.mnuDataTree_HumanApprovedAnalysisSummary_Hotlinks, $"Expected argument value of '{ContextMenuName.mnuDataTree_HumanApprovedAnalysisSummary_Hotlinks.ToString()}', but got '{hotlinksMenuId.ToString()}' instead.");

				var hotlinksMenuItemList = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);

				/*
				<menu id="mnuDataTree_HumanApprovedAnalysisSummary_Hotlinks">
					<item command="CmdInsertHumanApprovedAnalysis" />
				</menu>
				*/
				ToolStripMenuItemFactory.CreateHotLinkToolStripMenuItem(hotlinksMenuItemList, InsertHumanApprovedAnalysis_Click, TextAndWordsResources.Add_Approved_Analysis);

				return hotlinksMenuItemList;
			}

			private List<Tuple<ToolStripMenuItem, EventHandler>> Create_mnuDataTree_HumanApprovedAnalysis_Hotlinks(Slice slice, ContextMenuName hotlinksMenuId)
			{
				Require.That(hotlinksMenuId == ContextMenuName.mnuDataTree_HumanApprovedAnalysis_Hotlinks, $"Expected argument value of '{ContextMenuName.mnuDataTree_HumanApprovedAnalysis_Hotlinks.ToString()}', but got '{hotlinksMenuId.ToString()}' instead.");

				var hotlinksMenuItemList = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);

				/*
				<menu id="mnuDataTree_HumanApprovedAnalysis_Hotlinks">
					<item command="CmdShowHumanApprovedAnalysisConc" />
				</menu>
				*/
				ToolStripMenuItemFactory.CreateHotLinkToolStripMenuItem(hotlinksMenuItemList, ShowHumanApprovedAnalysisConc_Click, TextAndWordsResources.Assign_Analysis);

				return hotlinksMenuItemList;
			}

			private void ShowHumanApprovedAnalysisConc_Click(object sender, EventArgs e)
			{
				ShowConcDlg(Analysis);
			}

			private void ShowConcDlg(ICmObject concordOnObject)
			{
				using (var ctrl = new ConcordanceDlg(_majorFlexComponentParameters.StatusBar, concordOnObject))
				{
					ctrl.InitializeFlexComponent(_majorFlexComponentParameters.FlexComponentParameters);
					ctrl.ShowDialog(_propertyTable.GetValue<Form>(FwUtils.window));
				}
			}

			private void InsertHumanApprovedAnalysis_Click(object sender, EventArgs e)
			{
				using (var dlg = new EditMorphBreaksDlg(_propertyTable.GetValue<IHelpTopicProvider>(LanguageExplorerConstants.HelpTopicProvider)))
				{
					var wf = Wordform;
					if (wf == null)
					{
						return;
					}
					var tssWord = Wordform.Form.BestVernacularAlternative;
					var morphs = tssWord.Text;
					dlg.Initialize(tssWord, morphs, _cache.MainCacheAccessor.WritingSystemFactory, _cache, _dataTree.StyleSheet);
					// Making the form active fixes problems like LT-2619.
					// I'm (RandyR) not sure what adverse impact might show up by doing this.
					var mainWnd = _propertyTable.GetValue<Form>(FwUtils.window);
					mainWnd.Activate();
					if (dlg.ShowDialog(mainWnd) == DialogResult.OK)
					{
						morphs = dlg.GetMorphs().Trim();
						if (morphs.Length == 0)
						{
							return;
						}
						var prefixMarkers = MorphServices.PrefixMarkers(_cache);
						var postfixMarkers = MorphServices.PostfixMarkers(_cache);
						var allMarkers = prefixMarkers.ToList();
						foreach (var s in postfixMarkers)
						{
							if (!allMarkers.Contains(s))
							{
								allMarkers.Add(s);
							}
						}
						allMarkers.Add(" ");
						var breakMarkers = new string[allMarkers.Count];
						for (var i = 0; i < allMarkers.Count; ++i)
						{
							breakMarkers[i] = allMarkers[i];
						}
						var fullForm = MorphemeBreaker.DoBasicFinding(morphs, breakMarkers, prefixMarkers, postfixMarkers);
						UowHelpers.UndoExtension(TextAndWordsResources.Add_Approved_Analysis, _cache.ActionHandlerAccessor, () =>
						{
							var newAnalysis = _cache.ServiceLocator.GetInstance<IWfiAnalysisFactory>().Create();
							Wordform.AnalysesOC.Add(newAnalysis);
							newAnalysis.ApprovalStatusIcon = 1; // Make it human approved.
							var vernWS = TsStringUtils.GetWsAtOffset(tssWord, 0);
							foreach (var morph in fullForm.Split(Unicode.SpaceChars))
							{
								if (morph.Length != 0)
								{
									var mb = _cache.ServiceLocator.GetInstance<IWfiMorphBundleFactory>().Create();
									newAnalysis.MorphBundlesOS.Add(mb);
									mb.Form.set_String(vernWS, TsStringUtils.MakeString(morph, vernWS));
								}
							}
						});
					}
				}
			}

			private void CreateBrowseViewContextMenu()
			{
				// The actual menu declaration has a gazillion menu items, but only two of them are seen in this tool (plus the separator).
				// Start: <menu id="mnuBrowseView" (partial) >
				var contextMenuStrip = new ContextMenuStrip
				{
					Name = ContextMenuName.mnuBrowseView.ToString()
				};
				var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(2);

				// <command id="CmdWordformJumpToConcordance" label="Show Wordform in Concordance" message="JumpToTool">
				_wordformJumpToConcordanceMenu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _majorFlexComponentParameters.SharedEventHandlers.Get(AreaServices.JumpToTool), AreaResources.Show_Wordform_in_Concordance);
				_wordformJumpToConcordanceMenu.Tag = new List<object> { _majorFlexComponentParameters.FlexComponentParameters.Publisher, AreaServices.ConcordanceMachineName, _recordList };

				// <command id="CmdDeleteSelectedObject" label="Delete selected {0}" message="DeleteSelectedItem"/>
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, CmdDeleteSelectedObject_Clicked, string.Format(AreaResources.Delete_selected_0, StringTable.Table.GetString("WfiWordform", StringTable.ClassNames)));
				contextMenuStrip.Opening += ContextMenuStrip_Opening;

				// End: <menu id="mnuBrowseView" (partial) >
				_recordBrowseView.ContextMenuStrip = contextMenuStrip;
			}

			private void ContextMenuStrip_Opening(object sender, System.ComponentModel.CancelEventArgs e)
			{
				_recordBrowseView.ContextMenuStrip.Visible = !_recordList.HasEmptyList;
			}

			private void CmdDeleteSelectedObject_Clicked(object sender, EventArgs e)
			{
				_recordList.DeleteRecord(((ToolStripMenuItem)sender).Text, StatusBarPanelServices.GetStatusBarProgressPanel(_majorFlexComponentParameters.StatusBar));
			}

			#region Implementation of IDisposable
			private bool _isDisposed;

			~AnalysesToolMenuHelper()
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
					if (_analysisJumpToConcordanceMenu != null)
					{
						_analysisJumpToConcordanceMenu.Click -= _majorFlexComponentParameters.SharedEventHandlers.Get(AreaServices.JumpToTool);
						_analysisJumpToConcordanceMenu.Dispose();
					}
					if (_wordformJumpToConcordanceMenu != null)
					{
						_wordformJumpToConcordanceMenu.Click -= _majorFlexComponentParameters.SharedEventHandlers.Get(AreaServices.JumpToTool);
						_wordformJumpToConcordanceMenu.Dispose();
					}
					if (_recordBrowseView?.ContextMenuStrip != null)
					{
						_recordBrowseView.ContextMenuStrip.Opening -= ContextMenuStrip_Opening;
						_recordBrowseView.ContextMenuStrip.Dispose();
						_recordBrowseView.ContextMenuStrip = null;
					}
					_partiallySharedForToolsWideMenuHelper.Dispose();
					_fileExportMenuHelper.Dispose();
				}
				_majorFlexComponentParameters = null;
				_fileExportMenuHelper = null;
				_partiallySharedForToolsWideMenuHelper = null;
				_partiallySharedTextsAndWordsToolsMenuHelper = null;
				_recordBrowseView = null;
				_recordList = null;
				_dataTree = null;
				_wordformJumpToConcordanceMenu = null;
				_analysisJumpToConcordanceMenu = null;
				_propertyTable = null;
				_cache = null;
				_sharedEventHandlers = null;

				_isDisposed = true;
			}
			#endregion
		}
	}
}