// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.ComponentModel.Composition;
using System.Drawing;
using SIL.FieldWorks.Resources;

namespace LanguageExplorer.Areas.TextsAndWords.Tools.CorpusStatistics
{
	/// <summary>
	/// ITool implementation for the "corpusStatistics" tool in the "textsWords" area.
	/// </summary>
	[Export(AreaServices.TextAndWordsAreaMachineName, typeof(ITool))]
	internal sealed class CorpusStatisticsTool : ITool
	{
		private TextAndWordsAreaMenuHelper _textAndWordsAreaMenuHelper;
		private StatisticsView _statisticsView;
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
			_textAndWordsAreaMenuHelper.Dispose();

			// Remove StatisticsView (right panel of 'mainCollapsingSplitContainer').
			// Setting "SecondControl" to null will dispose "_statisticsView", so no need to do it here.
			majorFlexComponentParameters.MainCollapsingSplitContainer.SecondControl = null;
			_statisticsView = null;
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
			_textAndWordsAreaMenuHelper = new TextAndWordsAreaMenuHelper(majorFlexComponentParameters);
			_textAndWordsAreaMenuHelper.AddMenusForAllButConcordanceTool();

			// Get the StatisticsView into right panel of 'mainCollapsingSplitContainer'.
			_statisticsView = new StatisticsView(majorFlexComponentParameters);
		}

		/// <summary>
		/// Do whatever might be needed to get ready for a refresh.
		/// </summary>
		public void PrepareToRefresh()
		{ /* Do nothing. */ }

		/// <summary>
		/// Finish the refresh.
		/// </summary>
		public void FinishRefresh()
		{ /* Do nothing. */ }

		/// <summary>
		/// The properties are about to be saved, so make sure they are all current.
		/// Add new ones, as needed.
		/// </summary>
		public void EnsurePropertiesAreCurrent()
		{ /* Do nothing. */ }

		#endregion

		#region Implementation of IMajorFlexUiComponent

		/// <summary>
		/// Get the internal name of the component.
		/// </summary>
		/// <remarks>NB: This is the machine friendly name, not the user friendly name.</remarks>
		public string MachineName => AreaServices.CorpusStatisticsMachineName;

		/// <summary>
		/// User-visible localizable component name.
		/// </summary>
		public string UiName => "Statistics";
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
	}
}
