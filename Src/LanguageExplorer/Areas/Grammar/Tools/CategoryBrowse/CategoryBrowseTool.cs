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
using SIL.FieldWorks.Filters;
using SIL.FieldWorks.Resources;
using LanguageExplorer.Works;
using SIL.LCModel;
using SIL.LCModel.Application;

namespace LanguageExplorer.Areas.Grammar.Tools.CategoryBrowse
{
	/// <summary>
	/// ITool implementation for the "categoryBrowse" tool in the "grammar" area.
	/// </summary>
	[Export(AreaServices.GrammarAreaMachineName, typeof(ITool))]
	internal sealed class CategoryBrowseTool : ITool
	{
		private GrammarAreaMenuHelper _grammarAreaWideMenuHelper;
		private const string CategoriesWithoutTreeBarHandler = "categories_withoutTreeBarHandler";
		private PaneBarContainer _paneBarContainer;
		private RecordClerk _recordClerk;
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
			_grammarAreaWideMenuHelper.Dispose();
			PaneBarContainerFactory.RemoveFromParentAndDispose(majorFlexComponentParameters.MainCollapsingSplitContainer, ref _paneBarContainer);
			_grammarAreaWideMenuHelper = null;
		}

		/// <summary>
		/// Activate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the component that is becoming active.
		/// </remarks>
		public void Activate(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			if (_recordClerk == null)
			{
				_recordClerk = majorFlexComponentParameters.RecordClerkRepositoryForTools.GetRecordClerk(CategoriesWithoutTreeBarHandler, majorFlexComponentParameters.Statusbar, FactoryMethod);
			}
			_grammarAreaWideMenuHelper = new GrammarAreaMenuHelper(majorFlexComponentParameters, _recordClerk); // Use generic export event handler.

			_paneBarContainer = PaneBarContainerFactory.Create(
				majorFlexComponentParameters.FlexComponentParameters,
				majorFlexComponentParameters.MainCollapsingSplitContainer,
				new RecordBrowseView(XDocument.Parse(GrammarResources.GrammarCategoryBrowserParameters).Root, majorFlexComponentParameters.LcmCache, _recordClerk));
			RecordClerkServices.SetClerk(majorFlexComponentParameters, _recordClerk);
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
			_recordClerk.ReloadIfNeeded();
			((DomainDataByFlidDecoratorBase)_recordClerk.VirtualListPublisher).Refresh();
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
		public string MachineName => AreaServices.CategoryBrowseMachineName;

		/// <summary>
		/// User-visible localizable component name.
		/// </summary>
		public string UiName => "Categories Browse";

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

		private static RecordClerk FactoryMethod(LcmCache cache, FlexComponentParameters flexComponentParameters, string clerkId, StatusBar statusBar)
		{
			Require.That(clerkId == CategoriesWithoutTreeBarHandler, $"I don't know how to create a clerk with an ID of '{clerkId}', as I can only create on with an id of '{CategoriesWithoutTreeBarHandler}'.");

			return new RecordClerk(clerkId,
				statusBar,
				new PossibilityRecordList(cache.ServiceLocator.GetInstance<ISilDataAccessManaged>(), cache.LanguageProject.PartsOfSpeechOA),
				new PropertyRecordSorter("ShortName"),
				"Default",
				null,
				false,
				false);
		}
	}
}
