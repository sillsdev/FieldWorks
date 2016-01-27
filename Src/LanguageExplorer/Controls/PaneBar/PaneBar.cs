// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections;
using System.Linq;
using System.Windows.Forms;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorer.Controls.PaneBar
{
	/// <summary>
	/// Bar used to hold various controls, such as links, above the main content pane on the right of FLEx.
	/// </summary>
	internal partial class PaneBar : UserControl, IPaneBar
	{
		private Hashtable m_propertiesToWatch = new Hashtable();

		/// <summary>
		/// Constructor
		/// </summary>
		public PaneBar()
		{
			InitializeComponent();
			Dock = DockStyle.Top;
			AccessibleName = @"LanguageExplorer.Controls.PaneBar";
		}

		#region Implementation of IPaneBar

		/// <summary>
		/// Set the text of the pane bar.
		/// </summary>
		public override string Text
		{
			set
			{
				m_panelMain.Text = value;
				m_panelMain.Refresh();
			}
		}

		/// <summary>
		/// Refresh the pane bar display.
		/// </summary>
		public void RefreshPane()
		{
			foreach (var panelButton in m_panelMain.Controls.OfType<PanelButton>())
			{
				panelButton.UpdateDisplay();
			}
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
