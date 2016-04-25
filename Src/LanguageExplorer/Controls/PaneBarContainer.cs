// Copyright (c) 2002-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Utils;

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
	internal sealed partial class PaneBarContainer : UserControl, IPaneBarContainer, IFWDisposable, IPostLayoutInit
	{
		#region Data Members

		private XmlNode m_configurationParameters;
		private Control m_mainControl;
		private Size m_parentSizeHint;
		private string m_defaultPrintPaneId = "";
		private int instanceID;

		#endregion Data Members

		#region Construction

		/// <summary />
		public PaneBarContainer()
		{
			InitializeComponent();
		}

		/// <summary />
		internal PaneBarContainer(PaneBar.PaneBar paneBar, Control mainControl)
			: this()
		{
			SuspendLayout();
			m_mainControl = mainControl;
			PaneBar = paneBar;
			paneBar.Dock = DockStyle.Top;

			if (mainControl is IPaneBarUser)
			{
				((IPaneBarUser)mainControl).MainPaneBar = PaneBar;
			}

			Dock = DockStyle.Fill;
			mainControl.Dock = DockStyle.Fill;
			Controls.Add(paneBar);
			Controls.Add(m_mainControl);
			ResumeLayout(false);
			m_mainControl.BringToFront();
		}

		/// <summary />
		internal PaneBarContainer(Control mainControl)
			: this(new PaneBar.PaneBar(), mainControl)
		{
		}

#if __MonoCS__ // FWNX-425
		/// <summary> make Width always match parent Width </summary>
		protected override void OnLayout(LayoutEventArgs levent)
		{
			if (Parent != null && Width != Parent.Width)
			{
				Width = Parent.Width;
			}

			base.OnLayout (levent);
		}

		/// <summary> make Width always match parent Width </summary>
		protected override void OnSizeChanged(EventArgs e)
		{
			if (Parent != null && Width != Parent.Width)
			{
				Width = Parent.Width;
			}

			base.OnSizeChanged (e);
		}
#endif

		#endregion Construction

		#region Properties

		/// <summary>
		/// Used to give us an idea of what our boundaries will be before we are initialized
		/// enough to determine them ourselves.
		/// </summary>
		/// <remarks> at the moment, the top-level multipane is able to figure out its eventual
		/// size without help from this. However, multipanes inside of other ones rely on this.
		/// </remarks>
		internal Size ParentSizeHint
		{
			get { return m_parentSizeHint; }
			set { m_parentSizeHint = value; }
		}

		/// <summary>
		/// </summary>
		internal String DefaultPrintPaneId
		{
			get { return m_defaultPrintPaneId; }
			set { m_defaultPrintPaneId = value; }
		}

		/// <summary />
		internal Control MainControl
		{
			get { return m_mainControl; }
		}

		#endregion Properties

		#region IFWDisposable implementation, in part

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		#endregion IFWDisposable implementation, in part

		#region IMainUserControl implementation

		/// <summary />
		public string AccName
		{
			get
			{
				CheckDisposed();
				return "PaneBarContainer";
			}
		}

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
			CheckDisposed();

			return (m_mainControl as IMainContentControl).PrepareToGoAway();
		}

		/// <summary />
		public string AreaName
		{
			get
			{
				CheckDisposed();
				return (m_mainControl as IMainContentControl).AreaName;
			}
		}

		#endregion IMainContentControl implementation

		#region ICtrlTabProvider implementation

		/// <summary />
		public Control PopulateCtrlTabTargetCandidateList(List<Control> targetCandidates)
		{
			if (targetCandidates == null)
				throw new ArgumentNullException("'targetCandidates' is null.");

			// Don't bother with the IPaneBar.
			// Just check out the main control.
			return (m_mainControl as ICtrlTabProvider).PopulateCtrlTabTargetCandidateList(targetCandidates);
		}

		#endregion  ICtrlTabProvider implementation

		#region Other messages

		#endregion Other messages

		/// <summary />
		public void PostLayoutInit()
		{
			var initReceiver = MainControl as IPostLayoutInit;
			if (initReceiver != null)
				initReceiver.PostLayoutInit();
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

		public IPaneBar PaneBar { get; private set; }

		#endregion
	}
}
