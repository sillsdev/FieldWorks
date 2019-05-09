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
using LanguageExplorer.Controls.DetailControls;
using LanguageExplorer.Controls.PaneBar;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;
using SIL.LCModel;
using SIL.LCModel.Application;

namespace LanguageExplorer.Areas.Grammar.Tools.AdhocCoprohibEdit
{
	/// <summary>
	/// ITool implementation for the "AdhocCoprohibitionRuleEdit" tool in the "grammar" area.
	/// </summary>
	/// <remarks>
	/// This is the comment from the data model file about an "AdhocCoprohibition" (actual class "MoAdhocProhib":
	///
	/// This abstract class is intended to capture co-occurrence restrictions between morphemes or allomorphs which cannot be captured
	/// using morphosyntactic or phonological restrictions. Linguistically speaking, this is perhaps as bad a kludge as you can imagine.
	/// We allow it here for reasons of stealth-to-wealth work, with the understanding that the program will twist the user's arm into
	/// getting rid of these later on (perhaps by flagging each of these constraints with a Warning). The technique is borrowed from
	/// AMPLE's ad hoc pairs. Note however that, Aronoff (1976: 53) gives as an example of a negative constraint the fact that the
	/// suffix -ness does not attach to adjectives of the form X-ate, X-ant, or X-ent. So maybe this isn't such a kludge after all.
	/// On the other hand, Aronoff's examples are largely statistical generalizations, that is, tendencies - as opposed to hard constraints.
	///
	/// It may be that we should also have positive cooccurrence constraints. Aronoff (1976: 63) lists a number of "forms of the base"
	/// which are compatible with the English prefix un-, among them X-en (where -en is the past participle suffix), X-ing, and X-able,
	/// which would be examples of positive constraints. However, un- also attaches to a good many monomorphemic stems (roots, e.g. unhappy),
	/// so it may be that this is not a real generalization.
	///
	/// In addition to the attributes below, there should probably be an attribute to point to analyses which are ruled out by these constraints.
	/// These could be either grammatical words for which the parser would generate an incorrect analysis if it were not for this constraint,
	/// or ungrammatical words which the user has supplied, and which would be parsed if not for this constraint. It may even be desirable to
	/// allow individual constraints to be turned off in the parsing of such examples, in order to verify that the constraint works, and that
	/// it is (still) needed. However, the need for such an attr is probably more general than this class; see my email of 18 Jan 2000.
	/// </remarks>
	[Export(AreaServices.GrammarAreaMachineName, typeof(ITool))]
	internal sealed class AdhocCoprohibitionRuleEditTool : ITool
	{
		private AdhocCoprohibitionRuleEditToolMenuHelper _toolMenuHelper;
		private const string AdhocCoprohibitions = "adhocCoprohibitions";
		private MultiPane _multiPane;
		private RecordBrowseView _recordBrowseView;
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
			MultiPaneFactory.RemoveFromParentAndDispose(majorFlexComponentParameters.MainCollapsingSplitContainer, ref _multiPane);

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
				_recordList = majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue<IRecordListRepositoryForTools>(LanguageExplorerConstants.RecordListRepository).GetRecordList(AdhocCoprohibitions, majorFlexComponentParameters.StatusBar, FactoryMethod);
			}
			var root = XDocument.Parse(GrammarResources.AdhocCoprohibitionRuleEditToolParameters).Root;
			_recordBrowseView = new RecordBrowseView(root.Element("browseview").Element("parameters"), majorFlexComponentParameters.LcmCache, _recordList, majorFlexComponentParameters.UiWidgetController);
			var showHiddenFieldsPropertyName = PaneBarContainerFactory.CreateShowHiddenFieldsPropertyName(MachineName);
			var dataTree = new DataTree(majorFlexComponentParameters.SharedEventHandlers);
			_toolMenuHelper = new AdhocCoprohibitionRuleEditToolMenuHelper(majorFlexComponentParameters, this, dataTree, _recordList, _recordBrowseView, showHiddenFieldsPropertyName);
			var recordEditView = new RecordEditView(root.Element("recordview").Element("parameters"), XDocument.Parse(AreaResources.VisibilityFilter_All), majorFlexComponentParameters.LcmCache, _recordList, dataTree, majorFlexComponentParameters.UiWidgetController);
			var mainMultiPaneParameters = new MultiPaneParameters
			{
				Orientation = Orientation.Vertical,
				Area = _area,
				Id = "AdhocCoprohibItemsAndDetailMultiPane",
				ToolMachineName = MachineName
			};

			var recordEditViewPaneBar = new PaneBar();
			var panelButton = new PanelButton(majorFlexComponentParameters.FlexComponentParameters, null, showHiddenFieldsPropertyName, LanguageExplorerResources.ksShowHiddenFields, LanguageExplorerResources.ksShowHiddenFields)
			{
				Dock = DockStyle.Right
			};
			recordEditViewPaneBar.AddControls(new List<Control> { panelButton });

			_multiPane = MultiPaneFactory.CreateMultiPaneWithTwoPaneBarContainersInMainCollapsingSplitContainer(majorFlexComponentParameters.FlexComponentParameters,
				majorFlexComponentParameters.MainCollapsingSplitContainer, mainMultiPaneParameters,
				_recordBrowseView, "Browse", new PaneBar(), recordEditView, "Details", recordEditViewPaneBar);

			panelButton.MyDataTree = recordEditView.MyDataTree;
			// Too early before now.
			recordEditView.FinishInitialization();
			if (majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue(showHiddenFieldsPropertyName, false, SettingsGroup.LocalSettings))
			{
				majorFlexComponentParameters.FlexComponentParameters.Publisher.Publish("ShowHiddenFields", true);
			}
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
		public string MachineName => AreaServices.AdhocCoprohibitionRuleEditMachineName;

		/// <summary>
		/// User-visible localizable component name.
		/// </summary>
		public string UiName => "Ad hoc Rules";

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

		private static IRecordList FactoryMethod(LcmCache cache, FlexComponentParameters flexComponentParameters, string recordListId, StatusBar statusBar)
		{
			Require.That(recordListId == AdhocCoprohibitions, $"I don't know how to create a record list with an ID of '{recordListId}', as I can only create on with an id of '{AdhocCoprohibitions}'.");
			/*
            <clerk id="adhocCoprohibitions">
              <recordList owner="MorphologicalData" property="AdhocCoprohibitions" />
            </clerk>
			*/
			return new RecordList(recordListId, statusBar,
				cache.ServiceLocator.GetInstance<ISilDataAccessManaged>(), true,
				new VectorPropertyParameterObject(cache.LanguageProject.MorphologicalDataOA, "AdhocCoprohibitions", MoMorphDataTags.kflidAdhocCoProhibitions));
		}

		private sealed class AdhocCoprohibitionRuleEditToolMenuHelper : IDisposable
		{
			private MajorFlexComponentParameters _majorFlexComponentParameters;
			private DataTree _dataTree;
			private IRecordList _recordList;
			private RecordBrowseView _recordBrowseView;
			private string _extendedPropertyName;

			internal AdhocCoprohibitionRuleEditToolMenuHelper(MajorFlexComponentParameters majorFlexComponentParameters, ITool tool, DataTree dataTree, IRecordList recordList, RecordBrowseView recordBrowseView, string extendedPropertyName)
			{
				Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
				Guard.AgainstNull(tool, nameof(tool));
				Guard.AgainstNull(dataTree, nameof(dataTree));
				Guard.AgainstNull(recordList, nameof(recordList));
				Guard.AgainstNull(recordBrowseView, nameof(recordBrowseView));
				Guard.AgainstNullOrEmptyString(extendedPropertyName, nameof(extendedPropertyName));

				_majorFlexComponentParameters = majorFlexComponentParameters;
				_recordList = recordList;
				_recordBrowseView = recordBrowseView;
				_dataTree = dataTree;
				_extendedPropertyName = extendedPropertyName;

				var toolUiWidgetParameterObject = new ToolUiWidgetParameterObject(tool);
				//SetupUiWidgets(toolUiWidgetParameterObject);
				_majorFlexComponentParameters.UiWidgetController.AddHandlers(toolUiWidgetParameterObject);
				//CreateBrowseViewContextMenu();
			}

			#region IDisposable
			private bool _isDisposed;

			~AdhocCoprohibitionRuleEditToolMenuHelper()
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
					_recordBrowseView.ContextMenuStrip?.Dispose();
					_recordBrowseView.ContextMenuStrip = null;
				}
				_majorFlexComponentParameters = null;
				_dataTree = null;
				_recordList = null;
				_recordBrowseView = null;
				_extendedPropertyName = null;

				_isDisposed = true;
			}
			#endregion
		}
	}
}