// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.ComponentModel.Composition;
using System.Drawing;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Controls;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;
using LanguageExplorer.Works;
using SIL.LCModel;
using SIL.LCModel.Application;

namespace LanguageExplorer.Areas.Lexicon.Tools.BulkEditEntries
{
	/// <summary>
	/// ITool implementation for the "bulkEditEntriesOrSenses" tool in the "lexicon" area.
	/// </summary>
	[Export(AreaServices.LexiconAreaMachineName, typeof(ITool))]
	internal sealed class BulkEditEntriesOrSensesTool : ITool
	{
		private LexiconAreaMenuHelper _lexiconAreaMenuHelper;
		private const string EntriesOrChildren = "entriesOrChildren";
		private PaneBarContainer _paneBarContainer;
		private RecordBrowseView _recordBrowseView;
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
			_lexiconAreaMenuHelper.Dispose();
			PaneBarContainerFactory.RemoveFromParentAndDispose(majorFlexComponentParameters.MainCollapsingSplitContainer, ref _paneBarContainer);
			_lexiconAreaMenuHelper = null;
		}

		/// <summary>
		/// Activate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the component that is becoming active.
		/// </remarks>
		public void Activate(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			// Crashes in RecordList "CheckExpectedListItemsClassInSync" with: for some reason BulkEditBar.ExpectedListItemsClassId({0}) does not match SortItemProvider.ListItemsClass({1}).
			// BulkEditBar expected 5002, but
			// SortItemProvider was: 5035
			if (_recordList == null)
			{
				_recordList = majorFlexComponentParameters.RecordListRepositoryForTools.GetRecordList(EntriesOrChildren, majorFlexComponentParameters.Statusbar, FactoryMethod);
			}
			_lexiconAreaMenuHelper = new LexiconAreaMenuHelper(majorFlexComponentParameters, _recordList);

			var root = XDocument.Parse(LexiconResources.BulkEditEntriesOrSensesToolParameters).Root;
			var parametersElement = root.Element("parameters");
			parametersElement.Element("includeColumns").ReplaceWith(XElement.Parse(LexiconResources.LexiconBrowseDialogColumnDefinitions));
			OverrideServices.OverrideVisibiltyAttributes(parametersElement.Element("columns"), root.Element("overrides"));
			_recordBrowseView = new RecordBrowseView(parametersElement, majorFlexComponentParameters.LcmCache, _recordList);

			_paneBarContainer = PaneBarContainerFactory.Create(
				majorFlexComponentParameters.FlexComponentParameters,
				majorFlexComponentParameters.MainCollapsingSplitContainer,
				_recordBrowseView);
			RecordListServices.SetRecordList(majorFlexComponentParameters, _recordList);
			_lexiconAreaMenuHelper.Initialize();
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
		public string MachineName => AreaServices.BulkEditEntriesOrSensesMachineName;

		/// <summary>
		/// User-visible localizable component name.
		/// </summary>
		public string UiName => "CRASHES: Bulk Edit Entries";
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

		internal static IRecordList FactoryMethod(LcmCache cache, FlexComponentParameters flexComponentParameters, string recordListId, StatusBar statusBar)
		{
			Guard.AssertThat(recordListId == EntriesOrChildren, $"I don't know how to create a record list with an ID of '{recordListId}', as I can only create on with an id of '{EntriesOrChildren}'.");

			return new EntriesOrChildClassesRecordList(recordListId, statusBar, cache.ServiceLocator.GetInstance<ISilDataAccessManaged>(), cache.LanguageProject.LexDbOA);
		}
	}
}
