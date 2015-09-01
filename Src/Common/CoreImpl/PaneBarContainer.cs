// Copyright (c) 2002-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;
using SIL.Utils;

namespace SIL.CoreImpl
{
#if RANDYTODO
	// TODO: I think this can (ought?) be moved into LanguageExplorer, but the main window isn't in it yet either,
	// TODO: and FieldWorks and/or Framework, including FwApp and its two subclasses all need to be moved to do that.
#endif
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
	/// Most of the mehtods in these interfaces will be pass-through methods to m_mainControl,
	/// but we will try to get some use out of them, as well.
	/// </remarks>
	public partial class PaneBarContainer : BasicPaneBarContainer, IMainContentControl, IFWDisposable, IPostLayoutInit
	{
		#region Data Members

		private XmlNode m_configurationParameters;
		private Control m_mainControl;
		private Size m_parentSizeHint;
		private string m_defaultPrintPaneId = "";
		private int instanceID;

		#endregion Data Members

		#region Construction

		/// <summary>
		/// Constructor
		/// </summary>
		public PaneBarContainer()
		{
			InitializeComponent();
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

#if RANDYTODO // TODO: Needs an IPaneBar. Those impls seem to have been done in an old adapter assembly.
		/// <summary></summary>
		public void Init(Mediator mediator, IPropertyTable propertyTable, XmlNode configurationParameters)
		{
			CheckDisposed();

			SuspendLayout();

			m_mediator = mediator;
			m_propertyTable = propertyTable;
			m_configurationParameters = configurationParameters;

			// Make the IPaneBar.
			IPaneBar paneBar = CreatePaneBar();
			// initialize the panebar
			string groupId = XmlUtils.GetOptionalAttributeValue(m_configurationParameters, "PaneBarGroupId", null);
			if (groupId != null)
			{
				IFwMainWnd window = m_propertyTable.GetValue<IFwMainWnd>("window");
				ImageList.ImageCollection small = m_propertyTable.GetValue<ImageList.ImageCollection>("smallImages");
				paneBar.Init(small, (IUIMenuAdapter)window.MenuAdapter, m_mediator);
			}
			ReloadPaneBar(paneBar);
			m_paneBar = paneBar;
			Controls.Add(paneBar as Control);

			// Make the main control.
			XmlNode mainControlNode = m_configurationParameters.SelectSingleNode("control");
			Control mainControl = DynamicLoader.CreateObjectUsingLoaderNode(mainControlNode) as Control;
			if (mainControl == null)
				throw new ApplicationException("Something went wrong trying to create the main control.");

			if (!(mainControl is IMainContentControl))
				throw new ApplicationException("A PaneBarContainer can only handle controls which implement IMainContentControl.");

			mainControl.SuspendLayout();
			m_mainControl = mainControl;
			mainControl.Dock = DockStyle.Fill;
			if (mainControl is IPaneBarUser)
			{
				(mainControl as IPaneBarUser).MainPaneBar = paneBar;
			}
			(mainControl as IxCoreColleague).Init(m_mediator, m_propertyTable, mainControlNode.SelectSingleNode("parameters"));
#if __MonoCS__
			// At least one IPaneBarUser main control disposes of its MainPaneBar.  This can
			// cause the program to hang later on.  See FWNX-1036 for details.
			if ((m_paneBar as Control).IsDisposed)
			{
				Controls.Remove(m_paneBar as Control);
				m_paneBar = null;
			}
#endif
			Controls.Add(mainControl);
			if (mainControl is MultiPane)
			{
				MultiPane mp = mainControl as MultiPane;
				mp.DefaultPrintPaneId = DefaultPrintPaneId;
				mp.ParentSizeHint = ParentSizeHint;
				if (mp.FirstControl is IPaneBarUser)
					(mp.FirstControl as IPaneBarUser).MainPaneBar = paneBar;
			}
			mainControl.BringToFront();
			mainControl.ResumeLayout(false);
			ResumeLayout(false);
		}
#endif

		/// <summary>
		/// refresh (reload) the menu items on the PaneBar.
		/// </summary>
		public void RefreshPaneBar()
		{
			if (m_paneBar != null)
				ReloadPaneBar(m_paneBar);
		}

		private void ReloadPaneBar(IPaneBar paneBar)
		{
#if RANDYTODO
			string groupId = XmlUtils.GetOptionalAttributeValue(m_configurationParameters, "PaneBarGroupId", null);
			if (groupId != null)
			{
				IFwMainWnd window = m_propertyTable.GetValue<IFwMainWnd>("window");
				ChoiceGroup group = window.GetChoiceGroupForMenu(groupId);
				group.PopulateNow();
				paneBar.AddGroup(group);
			}
#endif
		}

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

		#region Implementation of IMainUserControl

		/// <summary>
		/// Get or set the name to be used by the accessibility object.
		/// </summary>
		string IMainUserControl.AccName { get; set; }

		#endregion
	}

#if RANDYTODO
	// TODO: I think this can (ought?) be moved into LanguageExplorer, but the main window isn't in it yet either,
	// TODO: and FieldWorks and/or Framework, including FwApp and its two subclasses all need to be moved to do that.
#endif
	/// <summary />
	public class BasicPaneBarContainer : UserControl
	{
		#region Data Members

		/// <summary />
		protected IPaneBar m_paneBar;

		#endregion Data Members

		/// <summary>
		/// Placement in the IPropertyTableProvider interface lets FwApp call IPropertyTable.DoStuff.
		/// </summary>
		public IPropertyTable PropertyTable { get; set; }

		/// <summary>
		/// Init for basic PaneBar.
		/// </summary>
		/// <param name="propertyTable"></param>
		/// <param name="mainControl"></param>
		/// <param name="paneBar"></param>
		public void Init(IPropertyTable propertyTable, Control mainControl, IPaneBar paneBar)
		{
			if (PropertyTable != null && PropertyTable != propertyTable)
				throw new ArgumentException("Mis-matched property tables being set for this object.");

			if (PropertyTable == null)
				PropertyTable = propertyTable;
			PaneBar = paneBar;
			Controls.Add(PaneBar as Control);

			mainControl.Dock = DockStyle.Fill;
			Controls.Add(mainControl);
			mainControl.BringToFront();
		}

		/// <summary />
		public IPaneBar PaneBar
		{
			get { return m_paneBar; }
			private set
			{
				if (m_paneBar != null)
				{
					throw new InvalidOperationException(@"Pane bar container already has a pane bar.");
				}
				m_paneBar = value;
				var pbAsControl = m_paneBar as Control;
				if (pbAsControl != null && pbAsControl.AccessibleName == null)
				{
					pbAsControl.AccessibleName = "XCore.PaneBar";
				}
			}
		}
	}
}
