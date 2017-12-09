// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.ComponentModel.Composition;
using System.Drawing;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.DetailControls;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Filters;
using SIL.FieldWorks.Resources;
using LanguageExplorer.Works;
using SIL.LCModel;
using SIL.LCModel.Application;

namespace LanguageExplorer.Areas.Grammar.Tools.LexiconProblems
{
	/// <summary>
	/// ITool implementation for the "lexiconProblems" tool in the "grammar" area.
	/// </summary>
	[Export(AreaServices.GrammarAreaMachineName, typeof(ITool))]
	internal sealed class LexiconProblemsTool : ITool
	{
		private GrammarAreaMenuHelper _grammarAreaWideMenuHelper;
		private const string LexProblems = "lexProblems";
		/// <summary>
		/// Main control to the right of the side bar control. This holds a RecordBar on the left and a PaneBarContainer on the right.
		/// The RecordBar on the left has no top PaneBar for information, menus, etc.
		/// </summary>
		private CollapsingSplitContainer _collapsingSplitContainer;
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
			_grammarAreaWideMenuHelper.Dispose();
			CollapsingSplitContainerFactory.RemoveFromParentAndDispose(majorFlexComponentParameters.MainCollapsingSplitContainer, ref _collapsingSplitContainer);
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
			var root = XDocument.Parse(GrammarResources.LexiconProblemsParameters).Root;
			if (_recordList == null)
			{
				_recordList = majorFlexComponentParameters.RecordListRepositoryForTools.GetRecordList(LexProblems, majorFlexComponentParameters.Statusbar, FactoryMethod);
			}
			_grammarAreaWideMenuHelper = new GrammarAreaMenuHelper(majorFlexComponentParameters, _recordList); // Use generic export event handler.

#if RANDYTODO
			// TODO: See LexiconEditTool for how to set up all manner of menus and toolbars.
#endif
			var dataTree = new DataTree();
			_collapsingSplitContainer = CollapsingSplitContainerFactory.Create(majorFlexComponentParameters.FlexComponentParameters,
				majorFlexComponentParameters.MainCollapsingSplitContainer,
				true,
				root.Element("parameters"),
				null,
				MachineName,
				majorFlexComponentParameters.LcmCache,
				_recordList,
				dataTree,
				MenuServices.GetFilePrintMenu(majorFlexComponentParameters.MenuStrip));
			RecordListServices.SetRecordList(majorFlexComponentParameters, _recordList);
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
		public string MachineName => AreaServices.LexiconProblemsMachineName;

		/// <summary>
		/// User-visible localizable component name.
		/// </summary>
		public string UiName => "Problems";

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
			Require.That(recordListId == LexProblems, $"I don't know how to create a record list with an ID of '{recordListId}', as I can only create on with an id of '{LexProblems}'.");
			/*
            <clerk id="lexProblems">
              <recordList owner="LangProject" property="Problems" />
              <filters>
                <filter label="Default" assemblyPath="Filters.dll" class="SIL.FieldWorks.Filters.ProblemAnnotationFilter" targetClasses="LexEntry, LexSense, PhEnvironment" />
              </filters>
              <sortMethods />
            </clerk>
			*/
			var probAnnFilter = new ProblemAnnotationFilter();
			probAnnFilter.Init(cache, XDocument.Parse(GrammarResources.LexiconProblemsParameters).Root.Element("filterElement"));
			return new RecordList(recordListId, statusBar,
				new PropertyRecordSorter("ShortName"), AreaServices.Default,
				probAnnFilter, true, true,
				cache.ServiceLocator.GetInstance<ISilDataAccessManaged>(), true,
				LangProjectTags.kflidAnnotations, cache.LanguageProject, "Problems");
		}
	}
}
