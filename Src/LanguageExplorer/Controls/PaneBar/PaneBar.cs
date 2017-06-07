// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorer.Controls.PaneBar
{
	/// <summary>
	/// Blue bar used to hold various controls, such as links, above main content panes on the right (of the side bar) of FLEx.
	///
	/// This class has one, and only one, child control, which is an instance of "PanelExtension" ("m_panelMain" data member).
	/// "m_panelMain" can then have one, or more, child controls, which are also instances of "PanelExtension", or one of its subclasses.
	///
	/// If the "Show Hidden Fields" checkbox is present, it is always on the far right.
	///
	/// The context menu is almost always on the far left, except the Lexicon Dictionary tool has two of them,
	/// in which case they are far left and far right.
	/// </summary>
	internal sealed partial class PaneBar : UserControl, IPaneBar
	{
		/// <summary />
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

		/// <summary>
		/// Add controls to the IPaneBar.
		/// </summary>
		/// <param name="paneBarControls">Controls to be added to IPaneBar.</param>
		public void AddControls(IList<Control> paneBarControls)
		{
			m_panelMain.Controls.AddRange(paneBarControls.ToArray());
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
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		public void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			FlexComponentCheckingService.CheckInitializationValues(flexComponentParameters, new FlexComponentParameters(PropertyTable, Publisher, Subscriber));

			PropertyTable = flexComponentParameters.PropertyTable;
			Publisher = flexComponentParameters.Publisher;
			Subscriber = flexComponentParameters.Subscriber;
		}

		#endregion
	}
}
