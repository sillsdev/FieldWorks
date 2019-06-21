// Copyright (c) 2015-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Drawing;
using SIL.Code;
using SIL.FieldWorks.Resources;

namespace LanguageExplorer.Areas.TextsAndWords.Tools.CorpusStatistics
{
	/// <summary>
	/// ITool implementation for the "corpusStatistics" tool in the "textsWords" area.
	/// </summary>
	[Export(AreaServices.TextAndWordsAreaMachineName, typeof(ITool))]
	internal sealed class CorpusStatisticsTool : ITool
	{
		private CorpusStatisticsToolMenuHelper _toolMenuHelper;
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
			// This will also remove any event handlers set up by the tool's UserControl instances that may have registered event handlers.
			majorFlexComponentParameters.UiWidgetController.RemoveToolHandlers();
			// Remove StatisticsView (right panel of 'mainCollapsingSplitContainer').
			// Setting "SecondControl" to null will dispose "_statisticsView", so no need to do it here.
			majorFlexComponentParameters.MainCollapsingSplitContainer.SecondControl = null;

			_toolMenuHelper.Dispose();
			_statisticsView = null;
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
			_toolMenuHelper = new CorpusStatisticsToolMenuHelper(majorFlexComponentParameters, this);
			// NB: Create the StatisticsView 'after' adding the tool handler, or you eat an exception for no tool registered.
			// Get the StatisticsView into right panel of 'mainCollapsingSplitContainer'. (The constructor does that.)
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
		public string UiName => AreaServices.CorpusStatisticsUiName;
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

		private sealed class CorpusStatisticsToolMenuHelper : IDisposable
		{
			private MajorFlexComponentParameters _majorFlexComponentParameters;
			private PartiallySharedTextsAndWordsToolsMenuHelper _partiallySharedTextsAndWordsToolsMenuHelper;

			internal CorpusStatisticsToolMenuHelper(MajorFlexComponentParameters majorFlexComponentParameters, ITool tool)
			{
				Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
				Guard.AgainstNull(tool, nameof(tool));

				_majorFlexComponentParameters = majorFlexComponentParameters;
				// Tool must be added, even when it adds no tool specific handlers.
				var toolUiWidgetParameterObject = new ToolUiWidgetParameterObject(tool);
				_partiallySharedTextsAndWordsToolsMenuHelper = new PartiallySharedTextsAndWordsToolsMenuHelper(majorFlexComponentParameters);
				_partiallySharedTextsAndWordsToolsMenuHelper.AddMenusForExpectedTextAndWordsTools(toolUiWidgetParameterObject);
				_majorFlexComponentParameters.UiWidgetController.AddHandlers(toolUiWidgetParameterObject);
#if RANDYTODO
				// TODO: See LexiconEditTool for how to set up all manner of menus and tool bars.
				// TODO: Set up factory method for the browse view.
#endif
			}

			#region Implementation of IDisposable
			private bool _isDisposed;

			~CorpusStatisticsToolMenuHelper()
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
				_partiallySharedTextsAndWordsToolsMenuHelper = null;

				_isDisposed = true;
			}
			#endregion
		}
	}
}