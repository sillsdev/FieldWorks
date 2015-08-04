using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;
using System.Reflection;
using SIL.CoreImpl;
using SIL.Utils; // For IFWDisposable

namespace XCore
{

	/// <summary>
	/// This class is used to provide a distinct place an IPaneBar control
	/// and a main control below it. The IPaneBar instance will be in the Pane11
	/// control of a SplitContainer, and the main control will be located in the Panel2 control.
	/// The splitter between them will not be movable. in fact, it should not be noticable
	/// </summary>
	/// <remarks>
	/// PaneBarContainer implements the IxCoreContentControl interface, which inludes its own methods,
	/// as well as 'extending the IXCoreUserControl, and IxCoreColleague interfaces.
	/// This 'extension' is really to ensure all of those interfaces are implemented,
	/// particularly for m_mainControl.
	///
	/// Most of the mehtods in these interfaces will be pass-through methods to m_mainControl,
	/// but we will try to get some use out of them, as well.
	/// </remarks>
	public partial class PaneBarContainer : BasicPaneBarContainer, IxCoreContentControl, IFWDisposable, IPostLayoutInit
	{
		#region Data Members

		private XmlNode m_configurationParameters;
		private Control m_mainControl;
		private Size m_parentSizeHint;
		private string m_defaultPrintPaneId = "";
		private int instanceID;

		#endregion Data Members

		#region Construction

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

		#region IxCoreColleague implementation
		/// <summary></summary>
		public void Init(Mediator mediator, PropertyTable propertyTable, XmlNode configurationParameters)
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
				XWindow window = m_propertyTable.GetValue<XWindow>("window");
				ImageCollection small = m_propertyTable.GetValue<ImageCollection>("smallImages");
				paneBar.Init(small, (IUIMenuAdapter)window.MenuAdapter, m_mediator);
			}
			ReloadPaneBar(paneBar);
			m_paneBar = paneBar;
			Controls.Add(paneBar as Control);

			// Make the main control.
			XmlNode mainControlNode = m_configurationParameters.SelectSingleNode("control");
			Control mainControl = DynamicLoader.CreateObjectUsingLoaderNode(mainControlNode) as Control;
			if (mainControl == null)
				throw new ApplicationException("Soemthing went wrong trying to create the main control.");

			if (!(mainControl is IxCoreContentControl))
				throw new ApplicationException("A PaneBarContainer can only handle controls which implement IxCoreContentControl.");

			mainControl.SuspendLayout();
			m_mainControl = mainControl;
			mainControl.Dock = DockStyle.Fill;
			if (mainControl is IPaneBarUser)
			{
				(mainControl as IPaneBarUser).MainPaneBar = paneBar;
			}
			/*
			if (mainControl is MultiPane)
			{
				MultiPane mp = mainControl as MultiPane;
				mp.DefaultPrintPaneId = DefaultPrintPaneId;
				mp.ParentSizeHint = ParentSizeHint;
			}*/
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
			string groupId = XmlUtils.GetOptionalAttributeValue(m_configurationParameters, "PaneBarGroupId", null);
			if (groupId != null)
			{
				XWindow window = m_propertyTable.GetValue<XWindow>("window");
				ChoiceGroup group = window.GetChoiceGroupForMenu(groupId);
				group.PopulateNow();
				paneBar.AddGroup(group);
			}
		}

		public IxCoreColleague[] GetMessageTargets()
		{
			CheckDisposed();

			return (m_mainControl as IxCoreColleague).GetMessageTargets();
		}

		/// <summary>
		/// Should not be called if disposed.
		/// </summary>
		public bool ShouldNotCall
		{
			get { return IsDisposed; }
		}

		/// <summary>
		/// Mediator message handling Priority
		/// </summary>
		public int Priority
		{
			get
			{
				return (int)ColleaguePriority.Medium;
			}
		}

		#endregion IxCoreColleague implementation

		#region IXCoreUserControl implementation

		public string AccName
		{
			get
			{
				CheckDisposed();
				return "PaneBarContainer";
			}
		}

		#endregion IXCoreUserControl implementation

		#region IxCoreContentControl implementation

		public bool PrepareToGoAway()
		{
			CheckDisposed();

			return (m_mainControl as IxCoreContentControl).PrepareToGoAway();
		}

		public string AreaName
		{
			get
			{
				CheckDisposed();
				return (m_mainControl as IxCoreContentControl).AreaName;
			}
		}

		#endregion IxCoreContentControl implementation

		#region IxCoreCtrlTabProvider implementation

		public Control PopulateCtrlTabTargetCandidateList(List<Control> targetCandidates)
		{
			if (targetCandidates == null)
				throw new ArgumentNullException("'targetCandidates' is null.");

			// Don't bother with the IPaneBar.
			// Just check out the main control.
			return (m_mainControl as IxCoreCtrlTabProvider).PopulateCtrlTabTargetCandidateList(targetCandidates);
		}

		#endregion  IxCoreCtrlTabProvider implementation

		#region Other messages

		#endregion Other messages

		public void PostLayoutInit()
		{
			var initReceiver = MainControl as IPostLayoutInit;
			if (initReceiver != null)
				initReceiver.PostLayoutInit();
		}
	}

	public partial class BasicPaneBarContainer : UserControl
	{
		#region Data Members

		protected Mediator m_mediator;
		protected PropertyTable m_propertyTable;
		protected IPaneBar m_paneBar;

		#endregion Data Members

		/// <summary>
		/// Init for basic panebar.
		/// </summary>
		/// <param name="mediator"></param>
		/// <param name="propertyTable"></param>
		/// <param name="mainControl"></param>
		public void Init(Mediator mediator, PropertyTable propertyTable, Control mainControl)
		{
			if (m_mediator != null && m_mediator != mediator)
				throw new ArgumentException("Mis-matched mediators being set for this object.");
			if (m_propertyTable != null && m_propertyTable != propertyTable)
				throw new ArgumentException("Mis-matched property tables being set for this object.");

			if (m_mediator == null)
				m_mediator = mediator;
			if (m_propertyTable == null)
				m_propertyTable = propertyTable;
			m_paneBar = CreatePaneBar();
			Controls.Add(m_paneBar as Control);

			mainControl.Dock = DockStyle.Fill;
			Controls.Add(mainControl);
			mainControl.BringToFront();
		}

		protected IPaneBar CreatePaneBar()
		{
			string preferredLibrary = m_propertyTable.GetValue("PreferredUILibrary", "xCoreOpenSourceAdapter.dll");
			Assembly adaptorAssembly = AdapterAssemblyFactory.GetAdapterAssembly(preferredLibrary);
			IPaneBar paneBar = adaptorAssembly.CreateInstance("XCore.PaneBar") as IPaneBar;
			Control pb = paneBar as Control;
			if (pb.AccessibleName == null)
				pb.AccessibleName = "XCore.PaneBar";
			pb.Dock = DockStyle.Top;
			return paneBar;
		}

		public IPaneBar PaneBar
		{
			get { return m_paneBar; }
		}
	}
}
