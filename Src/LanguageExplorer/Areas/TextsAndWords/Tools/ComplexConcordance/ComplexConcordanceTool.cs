// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Drawing;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;

namespace LanguageExplorer.Areas.TextsAndWords.Tools.ComplexConcordance
{
	/// <summary>
	/// ITool implementation for the "complexConcordance" tool in the "textsWords" area.
	/// </summary>
	internal sealed class ComplexConcordanceTool : ITool
	{
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
			TemporaryToolProviderHack.RemoveToolDisplay(majorFlexComponentParameters.MainCollapsingSplitContainer);
		}

		/// <summary>
		/// Activate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the component that is becoming active.
		/// </remarks>
		public void Activate(MajorFlexComponentParameters majorFlexComponentParameters)
		{
#if RANDYTODO
			// TODO: This tool need Itext's ConcordanceContainer main control and a gaggle of other stuff currently in IText.
#endif
			TemporaryToolProviderHack.SetupToolDisplay(majorFlexComponentParameters.MainCollapsingSplitContainer, this);
		}

		/// <summary>
		/// Do whatever might be needed to get ready for a refresh.
		/// </summary>
		public void PrepareToRefresh()
		{
#if RANDYTODO
			// TODO: Call PrepareToRefresh on nested RecordBrowseView control (left side of main ConcordanceContainer splitter control).
			// TODO: Call PrepareToRefresh on nested InterlinMasterNoTitleBar control (right side of main ConcordanceContainer splitter control).
#endif
		}

		/// <summary>
		/// Finish the refresh.
		/// </summary>
		public void FinishRefresh()
		{
#if RANDYTODO
			// TODO: If tool uses a SDA decorator (DomainDataByFlidDecoratorBase), then call its "Refresh" method.
			// TODO: Call "ReloadIfNeeded" on Record clerk(s).
#endif
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
		public string MachineName
		{
			get { return "complexConcordance"; }
		}

		/// <summary>
		/// User-visible localizable component name.
		/// </summary>
		public string UiName
		{
			get { return "Complex Concordance"; }
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
				var image = Images.SideBySideView;
				image.MakeTransparent(Color.Magenta);
				return image;
			}
		}

		#endregion
	}
}
