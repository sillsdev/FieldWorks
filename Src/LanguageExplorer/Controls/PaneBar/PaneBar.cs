// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Windows.Forms;
using SIL.CoreImpl;

namespace LanguageExplorer.Controls.PaneBar
{
#if RANDYTODO
	// TODO: This needs to be put into PaneBarContainer, which is currently in CoreImpl.
	// TODO: That is fine, but PaneBarContainer can't then create it directly, but we have to feed it into PaneBarContainer as IPaneBar.
#endif
	/// <summary>
	/// Bar used to hold various controls, such as links, above the main content pane on the right of FLEx.
	/// </summary>
	internal partial class PaneBar : UserControl, IPaneBar
	{
		private Hashtable m_propertiesToWatch = new Hashtable();

		public PaneBar()
		{
			InitializeComponent();
		}

		#region Implementation of IPaneBar

		/// <summary>
		/// Refresh the pane bar display.
		/// </summary>
		public void RefreshPane()
		{
			throw new NotImplementedException();
		}

		#endregion

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
		/// <param name="propertyTable">Interface to a property table.</param>
		/// <param name="publisher">Interface to the publisher.</param>
		/// <param name="subscriber">Interface to the subscriber.</param>
		public void InitializeFlexComponent(IPropertyTable propertyTable, IPublisher publisher, ISubscriber subscriber)
		{
			FlexComponentCheckingService.CheckInitializationValues(propertyTable, publisher, subscriber, PropertyTable, Publisher, Subscriber);

			PropertyTable = propertyTable;
			Publisher = publisher;
			Subscriber = subscriber;
		}

		#endregion
	}
}
