// Copyright (c) 2002-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;
using SIL.FieldWorks.Common.FwUtils;
using SIL.PlatformUtilities;

namespace LanguageExplorer.Controls
{
	/// <summary>
	/// This class is used to provide a distinct place an IPaneBar control
	/// and a main control below it. The IPaneBar instance will be in the Pane11
	/// control of a SplitContainer, and the main control will be located in the Panel2 control.
	/// The splitter between them will not be movable. in fact, it should not be noticable
	/// </summary>
	/// <remarks>
	/// PaneBarContainer implements the IMainContentControl interface, which inludes its own methods,
	/// as well as 'extending the IMainUserControl, and IxCoreColleague interfaces.
	/// This 'extension' is really to ensure all of those interfaces are implemented,
	/// particularly for m_mainControl.
	///
	/// Most of the methods in these interfaces will be pass-through methods to m_mainControl,
	/// but we will try to get some use out of them, as well.
	/// </remarks>
	internal sealed partial class PaneBarContainer : UserControl, IPaneBarContainer, IPostLayoutInit
	{
		#region Data Members

		private XmlNode m_configurationParameters;
		private Size m_parentSizeHint;
		private int instanceID;

		#endregion Data Members

		#region Construction

		/// <summary />
		public PaneBarContainer()
		{
			InitializeComponent();
		}

		/// <summary />
		internal PaneBarContainer(Control mainControl, PaneBar.PaneBar paneBar = null)
			: this()
		{
			if (paneBar == null)
			{
				paneBar = new PaneBar.PaneBar();
			}
			MainControl = mainControl;
			PaneBar = paneBar;
			paneBar.Dock = DockStyle.Top;
			if (mainControl is IPaneBarUser)
			{
				((IPaneBarUser)mainControl).MainPaneBar = PaneBar;
			}
			Dock = DockStyle.Fill;
			mainControl.Dock = DockStyle.Fill;
			Controls.Add(paneBar);
			Controls.Add(MainControl);
		}

		// FWNX-425
		/// <summary> make Width always match parent Width </summary>
		protected override void OnLayout(LayoutEventArgs levent)
		{
			if (Platform.IsMono)
			{
				if (Parent != null && Width != Parent.Width)
				{
					Width = Parent.Width;
				}
			}
			base.OnLayout(levent);
		}

		/// <summary> make Width always match parent Width </summary>
		protected override void OnSizeChanged(EventArgs e)
		{
			if (Platform.IsMono)
			{
				if (Parent != null && Width != Parent.Width)
				{
					Width = Parent.Width;
				}
			}
			base.OnSizeChanged(e);
		}

		#endregion Construction

		#region Properties

		/// <summary>
		/// Used to give us an idea of what our boundaries will be before we are initialized
		/// enough to determine them ourselves.
		/// </summary>
		/// <remarks> at the moment, the top-level multipane is able to figure out its eventual
		/// size without help from this. However, multipanes inside of other ones rely on this.
		/// </remarks>
		internal Size ParentSizeHint { get; set; }

		/// <summary>
		/// </summary>
		internal string DefaultPrintPaneId { get; set; } = string.Empty;

		/// <summary />
		internal Control MainControl { get; }

		#endregion Properties

		#region IMainUserControl implementation

		/// <summary />
		public string AccName => "PaneBarContainer";

		/// <summary>
		/// Get/set string that will trigger a message box to show.
		/// </summary>
		/// <remarks>Set to null or string.Empty to not show the message box.</remarks>
		public string MessageBoxTrigger { get; set; }

		#endregion IMainUserControl implementation

		#region IMainContentControl implementation

		/// <summary />
		public bool PrepareToGoAway()
		{
			return ((IMainContentControl)MainControl).PrepareToGoAway();
		}

		/// <summary />
		public string AreaName => ((IMainContentControl)MainControl).AreaName;

		#endregion IMainContentControl implementation

		#region ICtrlTabProvider implementation

		/// <summary />
		public Control PopulateCtrlTabTargetCandidateList(List<Control> targetCandidates)
		{
			if (targetCandidates == null)
			{
				throw new ArgumentNullException(nameof(targetCandidates));
			}
			// Don't bother with the IPaneBar.
			// Just check out the main control.
			return ((ICtrlTabProvider)MainControl).PopulateCtrlTabTargetCandidateList(targetCandidates);
		}

		#endregion  ICtrlTabProvider implementation

		/// <summary />
		public void PostLayoutInit()
		{
			var initReceiver = MainControl as IPostLayoutInit;
			initReceiver?.PostLayoutInit();
		}

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
			FlexComponentParameters.CheckInitializationValues(flexComponentParameters, new FlexComponentParameters(PropertyTable, Publisher, Subscriber));

			PropertyTable = flexComponentParameters.PropertyTable;
			Publisher = flexComponentParameters.Publisher;
			Subscriber = flexComponentParameters.Subscriber;

			PaneBar.InitializeFlexComponent(flexComponentParameters);
		}

		#endregion

		#region Implementation of IMainUserControl

		/// <summary>
		/// Get or set the name to be used by the accessibility object.
		/// </summary>
		string IMainUserControl.AccName { get; set; }

		#endregion

		#region Implementation of IPaneBarContainer

		public IPaneBar PaneBar { get; }

		#endregion
	}
}