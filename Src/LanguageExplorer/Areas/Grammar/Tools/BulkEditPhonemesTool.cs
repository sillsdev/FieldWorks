// Copyright (c) 2015-2020 SIL International
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
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.XMLViews;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Areas.Grammar.Tools
{
	/// <summary>
	/// ITool implementation for the "bulkEditPhonemes" tool in the "grammar" area.
	/// </summary>
	[Export(LanguageExplorerConstants.GrammarAreaMachineName, typeof(ITool))]
	internal sealed class BulkEditPhonemesTool : ITool
	{
		private BulkEditPhonemesToolMenuHelper _toolMenuHelper;
		private PaneBarContainer _paneBarContainer;
		private AssignFeaturesToPhonemes _assignFeaturesToPhonemesView;
		private IRecordList _recordList;

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
				_recordList = majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue<IRecordListRepositoryForTools>(LanguageExplorerConstants.RecordListRepository).GetRecordList(GrammarToolsServices.Phonemes, majorFlexComponentParameters.StatusBar, GrammarToolsServices.PhonemesFactoryMethod);
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
		public string MachineName => LanguageExplorerConstants.BulkEditPhonemesMachineName;

		/// <summary>
		/// User-visible localized component name.
		/// </summary>
		public string UiName => StringTable.Table.LocalizeLiteralValue(LanguageExplorerConstants.BulkEditPhonemesUiName);

		#endregion

		#region Implementation of ITool

		/// <summary>
		/// Get the area for the tool.
		/// </summary>
		[field: Import(LanguageExplorerConstants.GrammarAreaMachineName)]
		public IArea Area { get; private set; }

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
			private GrammarToolsServices _grammarToolsServices;

			internal BulkEditPhonemesToolMenuHelper(MajorFlexComponentParameters majorFlexComponentParameters, ITool tool, AssignFeaturesToPhonemes assignFeaturesToPhonemes, IRecordList recordList)
			{
				Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
				Guard.AgainstNull(tool, nameof(tool));
				Guard.AgainstNull(assignFeaturesToPhonemes, nameof(assignFeaturesToPhonemes));
				Guard.AgainstNull(recordList, nameof(recordList));

				_majorFlexComponentParameters = majorFlexComponentParameters;
				_assignFeaturesToPhonemesView = assignFeaturesToPhonemes;
				_recordList = recordList;
				SetupUiWidgets(tool);
				CreateBrowseViewContextMenu();
			}

			private void SetupUiWidgets(ITool tool)
			{
				_grammarToolsServices = new GrammarToolsServices();
				var toolUiWidgetParameterObject = new ToolUiWidgetParameterObject(tool);
				_grammarToolsServices.Setup_CmdInsertPhoneme(_majorFlexComponentParameters.LcmCache, toolUiWidgetParameterObject);
				_majorFlexComponentParameters.UiWidgetController.AddHandlers(toolUiWidgetParameterObject);
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
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, CmdPhonemeJumpToDefault_Clicked, LanguageExplorerResources.Show_in_Phonemes_Editor);
				ToolStripMenuItemFactory.CreateToolStripSeparatorForContextMenuStrip(contextMenuStrip);
				// <command id="CmdDeleteSelectedObject" label="Delete selected {0}" message="DeleteSelectedItem"/>
				ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, CmdDeleteSelectedObject_Clicked, string.Format(LanguageExplorerResources.Delete_selected_0, StringTable.Table.GetString("PhPhoneme", StringTable.ClassNames)));
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
				_recordList.DeleteRecord(string.Format(LanguageExplorerResources.Delete_selected_0, StringTable.Table.GetString("PhPhoneme", StringTable.ClassNames)), StatusBarPanelServices.GetStatusBarProgressPanel(_majorFlexComponentParameters.StatusBar));
			}

			private void CmdPhonemeJumpToDefault_Clicked(object sender, EventArgs e)
			{
				LinkHandler.PublishFollowLinkMessage(_majorFlexComponentParameters.FlexComponentParameters.Publisher, new FwLinkArgs(LanguageExplorerConstants.PhonemeEditMachineName, _recordList.CurrentObject.Guid));
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
				_grammarToolsServices = null;

				_isDisposed = true;
			}
			#endregion
		}

		/// <summary />
		private sealed class AssignFeaturesToPhonemes : RecordBrowseView
		{
			/// <summary>
			/// Required designer variable.
			/// </summary>
			private IContainer _components;

			/// <summary />
			internal AssignFeaturesToPhonemes(XElement browseViewDefinitions, LcmCache cache, IRecordList recordList, UiWidgetController uiWidgetController)
				: base(browseViewDefinitions, cache, recordList, uiWidgetController)
			{
				InitializeComponent();
			}

			#region Overrides of RecordBrowseView

			/// <summary>
			/// Initialize a FLEx component with the basic interfaces.
			/// </summary>
			/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
			public override void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
			{
				base.InitializeFlexComponent(flexComponentParameters);

				var bulkEditBar = BrowseViewer.BulkEditBar;
				// We want a custom name for the tab, the operation label, and the target item
				// Now we use good old List Choice.  bulkEditBar.ListChoiceTab.Text = LanguageExplorerResources.ksAssignFeaturesToPhonemes;
				bulkEditBar.OperationLabel.Text = LanguageExplorerResources.ksListChoiceDesc;
				bulkEditBar.TargetFieldLabel.Text = LanguageExplorerResources.ksTargetFeature;
				bulkEditBar.ChangeToLabel.Text = LanguageExplorerResources.ksChangeTo;
			}

			#endregion

			protected override BrowseViewer CreateBrowseViewer(XElement nodeSpec, int hvoRoot, LcmCache cache, ISortItemProvider sortItemProvider, ISilDataAccessManaged sda, UiWidgetController uiWidgetController)
			{
				return new BrowseViewerPhonologicalFeatures(nodeSpec, hvoRoot, cache, sortItemProvider, sda, uiWidgetController);
			}
			/// <summary>
			/// Clean up any resources being used.
			/// </summary>
			/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
			protected override void Dispose(bool disposing)
			{
				Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
				if (disposing)
				{
					_components?.Dispose();
				}
				base.Dispose(disposing);
			}

			#region Component Designer generated code

			/// <summary>
			/// Required method for Designer support - do not modify
			/// the contents of this method with the code editor.
			/// </summary>
			private void InitializeComponent()
			{
				_components = new Container();
				AutoScaleMode = AutoScaleMode.Font;
			}

			#endregion

			/// <summary>
			/// Browse viewer used for assigning phonological features to phonemes
			/// </summary>
			private sealed class BrowseViewerPhonologicalFeatures : BrowseViewer
			{
				/// <summary>
				/// The sortItemProvider is typically the RecordList that implements sorting and
				/// filtering of the items we are displaying.
				/// The data access passed typically is a decorator for the one in the cache, adding
				/// the sorted, filtered list of objects accessed as property madeUpFieldIdentifier of hvoRoot.
				/// </summary>
				public BrowseViewerPhonologicalFeatures(XElement nodeSpec, int hvoRoot, LcmCache cache, ISortItemProvider sortItemProvider, ISilDataAccessManaged sda, UiWidgetController uiWidgetController)
					: base(nodeSpec, hvoRoot, cache, sortItemProvider, sda, uiWidgetController)
				{ }

				///  <summary />
				protected override BulkEditBar CreateBulkEditBar(BrowseViewer bv, XElement spec, FlexComponentParameters flexComponentParameters, LcmCache cache)
				{
					return new BulkEditBarPhonologicalFeatures(bv, spec, flexComponentParameters, cache);
				}

				/// <summary>
				/// Bulk edit bar used for assigning phonological features to phonemes
				/// </summary>
				private sealed class BulkEditBarPhonologicalFeatures : BulkEditBar
				{
					/// <summary>
					/// Create one
					/// </summary>
					/// <param name="bv">The BrowseViewer that it is part of.</param>
					/// <param name="spec">The parameters element of the BV, containing the
					/// 'columns' elements that specify the BE bar (among other things).</param>
					/// <param name="flexComponentParameters"></param>
					/// <param name="cache"></param>
					public BulkEditBarPhonologicalFeatures(BrowseViewer bv, XElement spec, FlexComponentParameters flexComponentParameters, LcmCache cache) :
						base(bv, spec, flexComponentParameters, cache)
					{
						m_operationsTabControl.Controls.Remove(BulkCopyTab);
						m_operationsTabControl.Controls.Remove(ClickCopyTab);
						m_operationsTabControl.Controls.Remove(FindReplaceTab);
						m_operationsTabControl.Controls.Remove(TransduceTab);
						m_operationsTabControl.Controls.Remove(DeleteTab);
						if (m_listChoiceControl != null)
						{
							m_listChoiceControl.Text = string.Empty;
						}
						EnablePreviewApplyForListChoice();
					}

					private void BulkEditBarPhonologicalFeatures_EnableTargetFeatureCombo(object sender, TargetFeatureEventArgs e)
					{
						TargetCombo.Enabled = e.Enable;
					}

					protected override BulkEditItem MakeItem(XElement colSpec)
					{
						var bei = base.MakeItem(colSpec);
						if (bei == null)
						{
							return null;
						}
						if (bei.BulkEditControl is PhonologicalFeatureEditor phonologicalFeatureEditor)
						{
							phonologicalFeatureEditor.EnableTargetFeatureCombo += BulkEditBarPhonologicalFeatures_EnableTargetFeatureCombo;
						}
						return bei;
					}

					protected override void ShowPreviewItems(ProgressState state)
					{
						m_bv.BrowseView.Vc.MultiColumnPreview = false;
						var itemsToChange = ItemsToChange(false);
						var bei = m_beItems[m_itemIndex];
						if (!(bei.BulkEditControl is PhonologicalFeatureEditor phonologicalFeatureEditor))
						{
							// User chose to remove the targeted feature
							bei.BulkEditControl.FakeDoit(itemsToChange, XMLViewsDataCache.ktagAlternateValue, XMLViewsDataCache.ktagItemEnabled, state);
						}
						else
						{
							if (!phonologicalFeatureEditor.SelectedItemIsFsFeatStruc)
							{
								// User chose one of the values of the targeted feature
								phonologicalFeatureEditor.FakeDoit(itemsToChange, XMLViewsDataCache.ktagAlternateValue, XMLViewsDataCache.ktagItemEnabled, state);
							}
							else
							{
								// User built a FsFeatStruc with the features and values to change.
								// This means we have to find the columns for each feature in the FsFeatStruc and
								// then show the change for that feature in that column.
								var selectedHvo = phonologicalFeatureEditor.SelectedHvo;
								var selectedLabel = phonologicalFeatureEditor.SelectedLabel;
								var featureValuePairs = phonologicalFeatureEditor.FeatureValuePairsInSelectedFeatStruc;
								var featureAbbreviations = featureValuePairs.Select(s =>
								{
									var i = s.IndexOf(":");
									return s.Substring(0, i);
								});
								m_bv.BrowseView.Vc.MultiColumnPreview = true;
								for (var iColumn = 0; iColumn < m_beItems.Count(); iColumn++)
								{
									if (m_beItems[iColumn] == null)
									{
										continue;
									}
									var pfe = m_beItems[iColumn].BulkEditControl as PhonologicalFeatureEditor;
									if (pfe == null)
									{
										continue;
									}
									pfe.ClearPreviousPreviews(itemsToChange, XMLViewsDataCache.ktagAlternateValueMultiBase + iColumn + 1);
									if (!featureAbbreviations.Contains(pfe.FeatDefnAbbr))
									{
										continue;
									}
									var tempSelectedHvo = pfe.SelectedHvo;
									pfe.SelectedHvo = selectedHvo;
									var tempSelectedLabel = pfe.SelectedLabel;
									pfe.SelectedLabel = selectedLabel;
									pfe.FakeDoit(itemsToChange, XMLViewsDataCache.ktagAlternateValueMultiBase + iColumn + 1, XMLViewsDataCache.ktagItemEnabled, state);
									pfe.SelectedHvo = tempSelectedHvo;
									pfe.SelectedLabel = tempSelectedLabel;
								}
							}
						}
					}

					protected override void Dispose(bool disposing)
					{
						Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
						if (IsDisposed)
						{
							// No need to run it more than once.
							return;
						}

						if (disposing)
						{
							foreach (var bei in m_beItems)
							{
								if (bei?.BulkEditControl is PhonologicalFeatureEditor phonologicalFeatureEditor)
								{
									phonologicalFeatureEditor.EnableTargetFeatureCombo -= BulkEditBarPhonologicalFeatures_EnableTargetFeatureCombo;
								}
							}
						}

						base.Dispose(disposing);
					}
				}
			}
		}
	}
}