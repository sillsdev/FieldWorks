// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Drawing;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;

namespace LanguageExplorer.Areas.TextsAndWords.Tools.CorpusStatistics
{
	/// <summary>
	/// ITool implementation for the "corpusStatistics" tool in the "textsWords" area.
	/// </summary>
	internal sealed class CorpusStatisticsTool : ITool
	{
		private StatisticsView _statisticsView;

		#region Implementation of IPropertyTableProvider

		/// <summary>
		/// Placement in the IPropertyTableProvider interface lets FwApp call IPropertyTable.DoStuff.
		/// </summary>
		public IPropertyTable PropertyTable { get; private set; }

		#endregion

		#region Implementation of IPublisherProvider

		/// <summary>
		/// Get the IPublisher.
		/// </summary>
		public IPublisher Publisher { get; private set; }

		#endregion

		#region Implementation of ISubscriberProvider

		/// <summary>
		/// Get the ISubscriber.
		/// </summary>
		public ISubscriber Subscriber { get; private set; }

		#endregion

		#region Implementation of IFlexComponent

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		public void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			FlexComponentCheckingService.CheckInitializationValues(flexComponentParameters, new FlexComponentParameters(PropertyTable, Publisher, Subscriber));

			PropertyTable = flexComponentParameters.PropertyTable;
			Publisher = flexComponentParameters.Publisher;
			Subscriber = flexComponentParameters.Subscriber;
		}

		#endregion

		#region Implementation of IMajorFlexComponent

		/// <summary>
		/// Deactivate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the outgoing component, when the user switches to a component.
		/// </remarks>
		public void Deactivate(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			_statisticsView.Deactivate(majorFlexComponentParameters);

			// Remove StatisticsView (right panel of 'mainCollapsingSplitContainer').
			majorFlexComponentParameters.MainCollapsingSplitContainer.SecondControl = null;
			_statisticsView = null;
		}

		/// <summary>
		/// Activate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the component that is becoming active.
		/// </remarks>
		public void Activate(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			// Get the StatisticsView into right panel of 'mainCollapsingSplitContainer'.
			_statisticsView = new StatisticsView();
			majorFlexComponentParameters.MainCollapsingSplitContainer.SecondControl = _statisticsView;
			_statisticsView.InitializeFlexComponent(majorFlexComponentParameters.FlexComponentParameters);
			_statisticsView.Activate(majorFlexComponentParameters);
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
		{
		}

		#endregion

		#region Implementation of IMajorFlexUiComponent

		/// <summary>
		/// Get the internal name of the component.
		/// </summary>
		/// <remarks>NB: This is the machine friendly name, not the user friendly name.</remarks>
		public string MachineName
		{
			get { return "corpusStatistics"; }
		}

		/// <summary>
		/// User-visible localizable component name.
		/// </summary>
		public string UiName
		{
			get { return "Statistics"; }
		}

		#endregion

		#region Implementation of ITool

		/// <summary>
		/// Get the area machine name the tool is for.
		/// </summary>
		public string AreaMachineName
		{
			get { return "textsWords"; }
		}

		/// <summary>
		/// Get the image for the area.
		/// </summary>
		public Image Icon
		{
			get
			{
				var image = Images.DocumentView;
				image.MakeTransparent(Color.Magenta);
				return image;
			}
		}

		#endregion
	}
}
