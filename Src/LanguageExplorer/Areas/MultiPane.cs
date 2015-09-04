using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;
using LanguageExplorer.Controls;
using SIL.CoreImpl;
using SIL.Utils;

namespace LanguageExplorer.Areas
{
	/// <summary>
	/// A MultiPane (actually currently more a DualPane) displays two child controls,
	/// either side by side or one above the other, with a splitter between them.
	///
	/// The vertical parameter causes the two controls to be one above the other, if true,
	/// or side by side, if false. (Default, if omitted, is true.)
	/// The id parameter gives the MultiPane a name (which should be unique across the whole
	/// containing application) to use in storing state, such as the position of the splitter,
	/// persistently.
	/// It is mandatory to specify the area that the control is part of, but I (JT) don't know why.
	///
	/// If the mediator has a property called id_ShowFirstPane (e.g., LexEntryAndEditor_ShowFirstPane),
	/// it will control the visibility of the first pane (visible if the property is true).
	/// </summary>
	internal sealed class MultiPane : CollapsingSplitContainer, IMainContentControl
	{
		/// <summary />
		internal event EventHandler ShowFirstPaneChanged;

		// When its superclass gets switched to the new SplitContainer class. it has to implement IMainUserControl itself.
		private IContainer components;
		private bool m_prioritySecond; // true to give second pane first chance at broadcast messages.
		private Size m_parentSizeHint;
		private bool m_showingFirstPane;
		private string m_propertyControllingVisibilityOfFirstPane;
		// true to suppress collapsing fill pane below minimum
		// It doesn't go on the superclass,
		// because we will use its builtin Panel2MinSize property to actually control this,
		// and rumor has it that the min can't be known for some controls, until after a layout.
		// So, we have to 'remember' what to do in this data member.
		// Technically, we could just look it up in the config file,
		// but this will be faster.
//		private bool m_fDontCollapseFillPane = false;
		private string m_defaultPrintPaneId = "";
		private string m_defaultFocusControl = "";
		private XmlNode m_configurationParameters;
		//the name of the tool which this MultiPane is a part of.
		private string toolName = "";

		/// <summary>
		/// Constructor
		/// </summary>
		internal MultiPane()
		{
			ResetSplitterEventHandler(false); // Get rid of the handler until we have a parent.

			m_parentSizeHint.Width = m_parentSizeHint.Height = 0;

			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
		}

		/// <summary />
		internal string PropertyControllingVisibilityOfFirstPane
		{
			get { return m_propertyControllingVisibilityOfFirstPane; }
			set
			{
				if (!string.IsNullOrEmpty(m_propertyControllingVisibilityOfFirstPane))
				{
					Subscriber.Unsubscribe(m_propertyControllingVisibilityOfFirstPane, PropertyControllingVisibilityOfFirstPane_Changed);
				}
				m_propertyControllingVisibilityOfFirstPane = value;
				m_showingFirstPane = string.IsNullOrEmpty(value);
				Subscriber.Subscribe(m_propertyControllingVisibilityOfFirstPane, PropertyControllingVisibilityOfFirstPane_Changed);
			}
		}

		private void PropertyControllingVisibilityOfFirstPane_Changed(object newValue)
		{
			var showIt = (bool) newValue;
			if (m_showingFirstPane == showIt)
			{
				return; // No change.
			}
			m_showingFirstPane = string.IsNullOrEmpty(m_propertyControllingVisibilityOfFirstPane) && showIt;
			Panel1Collapsed = !m_showingFirstPane;
			if (ShowFirstPaneChanged != null)
				ShowFirstPaneChanged(this, new EventArgs());
		}

		/// <summary />
		internal string PrintPane
		{
			get
			{
				CheckDisposed();
				return m_defaultPrintPaneId;
			}
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;
			if (disposing)
			{
				if(components != null)
					components.Dispose();
			}

			base.Dispose(disposing);
		}

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
//			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(MultiPane));
			//
			// MultiPane
			//
			this.Name = "MultiPane";

		}
		#endregion

		#region IMainUserControl implementation

		/// <summary>
		/// This is the property that return the name to be used by the accessibility object.
		/// </summary>
		public string AccName
		{
			get
			{
				CheckDisposed();

				string defaultName = "MultiPane";
				string name;

#if RANDYTO
				if (this is IxCoreColleague)
				{
					name = XmlUtils.GetOptionalAttributeValue(m_configurationParameters, "persistContext");

					if (name == null || name == "")
						name = XmlUtils.GetOptionalAttributeValue(m_configurationParameters, "id");

					if (name == null || name == "")
						name = XmlUtils.GetOptionalAttributeValue(m_configurationParameters, "label", defaultName);
				}
				else
#endif
					name = defaultName;

				return name;
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

			//we are ready to go away if our two controls are ready to go away
			bool firstControlReady = true;
			if (FirstControl != null)
				firstControlReady = ((IMainContentControl)FirstControl).PrepareToGoAway();
			bool secondControlReady = true;
			if (SecondControl != null)
				secondControlReady = ((IMainContentControl)SecondControl).PrepareToGoAway();

			return firstControlReady && secondControlReady;
		}

		/// <summary />
		public string AreaName
		{
			get
			{
				CheckDisposed();

				return XmlUtils.GetManditoryAttributeValue( m_configurationParameters, "area");
			}
		}

		#endregion // IMainContentControl implementation

		#region ICtrlTabProvider implementation

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "result is a reference")]
		public Control PopulateCtrlTabTargetCandidateList(List<Control> targetCandidates)
		{
			if (targetCandidates == null)
				throw new ArgumentNullException("'targetCandidates' is null.");

			int sizeOfSharedDimensionPanel1 = Orientation == Orientation.Vertical ? Panel1.Width : Panel1.Height;
			Control result = null;
			if (sizeOfSharedDimensionPanel1 != CollapsingSplitContainer.kCollapsedSize && !Panel1Collapsed)
			{
				// Panel1 is visible and wide.
				result = (FirstControl as ICtrlTabProvider).PopulateCtrlTabTargetCandidateList(targetCandidates);
				if (!FirstControl.ContainsFocus)
					result = null;
			}
			int sizeOfSharedDimensionPanel2 = Orientation == Orientation.Vertical ? Panel2.Width : Panel2.Height;
			if (sizeOfSharedDimensionPanel2 != CollapsingSplitContainer.kCollapsedSize && !Panel2Collapsed)
			{
				// Panel2 is visible and wide.
				Control otherResult = (SecondControl as ICtrlTabProvider).PopulateCtrlTabTargetCandidateList(targetCandidates);
				if (SecondControl.ContainsFocus)
				{
					Debug.Assert(result == null, "result is unexpectedly not null.");
					Debug.Assert(otherResult != null, "otherResult is unexpectedly null.");
					result = otherResult;
				}
			}

			return result;
		}

		#endregion  ICtrlTabProvider implementation

		#region IxCoreColleague implementation
#if RANDYTODO
		/// <summary>
		/// Initialize this has an IxCoreColleague
		/// </summary>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		public void Init(Mediator mediator, IPropertyTable propertyTable, XmlNode configurationParameters)
		{
			CheckDisposed();

			SuspendLayout();
			Panel1.SuspendLayout();
			Panel2.SuspendLayout();
			IsInitializing = true;

			m_configurationParameters = configurationParameters;
			m_mediator = mediator;
			m_propertyTable = propertyTable;
			var toolNode = configurationParameters.SelectSingleNode("ancestor::tool");
			toolName = toolNode == null ? "" : toolNode.Attributes["value"].Value;
//			m_fDontCollapseFillPane = XmlUtils.GetOptionalBooleanAttributeValue(
//				m_configurationParameters, "dontCollapseFillPane", false);

			XmlNodeList nodes = configurationParameters.SelectNodes("control");
			if (nodes.Count != 2)
				throw new ConfigurationException(
					"Was expecting 2 controls to be defined in the parameters of the Multipane.",
					configurationParameters);

			string id = XmlUtils.GetAttributeValue(configurationParameters, "id", "");
			m_propertyControllingVisibilityOfFirstPane = GetPropertyControllingVisibilityOfFirstPane(nodes[0]);
			if (m_propertyControllingVisibilityOfFirstPane == null)
			{
				m_showingFirstPane = false;
			}
			else
			{
				m_showingFirstPane = true; // default
				// NOTE: we don't actually want to create and persist this property if it's not already loaded.
				bool showingFirstPane;
				if (m_propertyTable.TryGetValue(m_propertyControllingVisibilityOfFirstPane, SettingsGroup.LocalSettings, out showingFirstPane))
				{
					m_showingFirstPane = showingFirstPane;
				}
			}

			SplitterWidth = 5;
			if (id != "") //must have an id if we're going to persist the value of the splitter
				this.Name = id;//for debugging
			FirstLabel = XmlUtils.GetOptionalAttributeValue(configurationParameters, "firstLabel", "");
			SecondLabel = XmlUtils.GetOptionalAttributeValue(configurationParameters, "secondLabel", "");
			SetFirstCollapseZone(nodes[0]);
			SetSecondCollapseZone(nodes[1]);

			string orientation = XmlUtils.GetOptionalAttributeValue(configurationParameters, "splitterBarOrientation", "vertical");
			if (orientation.ToLowerInvariant() == "horizontal" && Orientation != Orientation.Horizontal)
				Orientation = Orientation.Horizontal;
			else if (Orientation != Orientation.Vertical)
				Orientation = Orientation.Vertical;

			m_prioritySecond = XmlUtils.GetOptionalBooleanAttributeValue(configurationParameters,
				"prioritySecond", false);
			string defaultPrintPaneId = XmlUtils.GetOptionalAttributeValue(configurationParameters,
				"defaultPrintPane", "");
			string defaultFocusControl = XmlUtils.GetOptionalAttributeValue(configurationParameters,
				"defaultFocusControl", "");
			// If we are a subcontrol of a MultiPane, our DefaultPrintPane property may already be set.
			// we don't want to change it, unless it's not an empty string.
			if (!String.IsNullOrEmpty(defaultPrintPaneId))
				m_defaultPrintPaneId = defaultPrintPaneId;
			if (!String.IsNullOrEmpty(defaultFocusControl))
				m_defaultFocusControl = defaultFocusControl;
			MakeSubControl(nodes[0], Size, true);
			Panel1Collapsed = !m_showingFirstPane;
			MakeSubControl(nodes[1], Size, false);

			// Attempt to focus the default child control if there is one configured
			// TODO: Things are not yet in a suitable state, hooking onto a later event should work
			// TODO: But if you switch between tools in an area there is sometimes an extra
			// TODO: WM_LBUTTON_DOWN event which steals focus back into the ListViewItemArea
			SetFocusInDefaultControl();

			IsInitializing = false;
			Panel2.ResumeLayout(false);
			Panel1.ResumeLayout(false);
			ResumeLayout(false);

			//it's important to do this last, so that we don't go generating property change
			//notifications that we then go trying to cope with before we are ready
			mediator.AddColleague(this);
			m_fOkToPersistSplit = true;
		}

		private string GetPropertyControllingVisibilityOfFirstPane(XmlNode configurationNodeOfFirstPane)
		{
			XmlNode parameters = configurationNodeOfFirstPane.SelectSingleNode("parameters");
			if (parameters == null)
				return null;
			string property =  XmlUtils.GetOptionalAttributeValue(parameters,"id", null);
			if(property == null)
				return null;
			return property.Insert(0,"Show_");
		}
#endif

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

		/// <summary>
		/// Size is overridden so that until the pane is sized properly,
		/// typically docked in some parent, it will be big enough not to interfere with
		/// the splitter position set in its Init method.
		/// </summary>
		protected override Size DefaultSize
		{
			get
			{
				return new Size(2000,2000);
			}
		}

#if RANDYTODO
		private void MakeSubControl(XmlNode configuration, Size parentSizeHint, bool isFirst)
		{
			XmlNode dynLoaderNode = configuration.SelectSingleNode("dynamicloaderinfo");
			if (dynLoaderNode == null)
				throw new ArgumentException("Required 'dynamicloaderinfo' XML node not found, while trying to make control for MultiPane.", "configuration");

			string contentAssemblyPath = XmlUtils.GetManditoryAttributeValue(dynLoaderNode, "assemblyPath");
			string contentClass = XmlUtils.GetManditoryAttributeValue(dynLoaderNode, "class");
			try
			{
				Control subControl = (Control)DynamicLoader.CreateObject(contentAssemblyPath, contentClass);
				if (subControl.AccessibleName == null)
					subControl.AccessibleName = contentClass;
				if (!(subControl is IxCoreColleague))
				{
					throw new ApplicationException(
						"XCore can only handle controls which implement IxCoreColleague. " +
						contentClass + " does not.");
				}
				if (!(subControl is IMainUserControl))
				{
					throw new ApplicationException(
						"XCore can only handle controls which implement IMainUserControl. " +
						contentClass + " does not.");
				}

				subControl.SuspendLayout();

				subControl.Dock = DockStyle.Fill;

				// we add this before Initializing so that this child control will have access
				// to its eventual height and width, in case it needs to make initialization
				// decisions based on that.  for example, if the child is another multipane, it
				// will use this to come up with a reasonable default location for its splitter.
				if (subControl is MultiPane)
				{
					MultiPane mpSubControl = subControl as MultiPane;
					mpSubControl.ParentSizeHint = parentSizeHint;
					// cause our subcontrol to inherit our DefaultPrintPane property.
					mpSubControl.DefaultPrintPaneId = m_defaultPrintPaneId;
				}
				// we add this before Initializing so that this child control will have access
				// to its eventual height and width, in case it needs to make initialization
				// decisions based on that.  for example, if the child is another multipane, it
				// will use this to come up with a reasonable default location for its splitter.
				if (subControl is PaneBarContainer)
				{
					PaneBarContainer mpSubControl = subControl as PaneBarContainer;
					mpSubControl.ParentSizeHint = parentSizeHint;
					// cause our subcontrol to inherit our DefaultPrintPane property.
					mpSubControl.DefaultPrintPaneId = m_defaultPrintPaneId;
				}


				XmlNode parameters = null;
				if (configuration != null)
					parameters = configuration.SelectSingleNode("parameters");
				((IxCoreColleague)subControl).Init(m_mediator, m_propertyTable, parameters);

				// in normal situations, colleagues add themselves to the mediator when
				// initialized.  in this case, we don't want this colleague to add itself
				// because we want it to be subservient to this "papa" control.  however, since
				// this control is only experimental, I'm loathe to change the interfaces in
				// such a way as to tell a colleague that it should not add itself to the
				// mediator.  so, for now, we will just do this hack and remove the colleague
				// from the mediator.
				m_mediator.RemoveColleague((IxCoreColleague)subControl);

				if (isFirst)
				{
					subControl.AccessibleName += ".First";
					FirstControl = subControl;
				}
				else
				{
					subControl.AccessibleName += ".Second";
					SecondControl = subControl;
				}
				subControl.ResumeLayout(false);
			}
			catch (Exception error)
			{
				string s = "Something went wrong trying to create a " + contentClass + ".";
				IFwMainWnd window = m_propertyTable.GetValue<IFwMainWnd>("window");
				ErrorReporter.ReportException(new ApplicationException(s, error),
					window.ApplicationRegistryKey, m_propertyTable.GetValue<IFeedbackInfoProvider>("FeedbackInfoProvider").SupportEmailAddress);
			}
		}
#endif

		#endregion // IxCoreColleague implementation

		/// <summary />
		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);

			Control parent = Parent;
			if (parent != null && PropertyTable != null)
			{
				ResetSplitterEventHandler(true);
				SetSplitterDistance();
			}
		}

		private string SplitterDistancePropertyName
		{
			get
			{
				return String.Format("MultiPaneSplitterDistance_{0}_{1}_{2}",
					m_configurationParameters.Attributes["area"].Value,
					PropertyTable.GetValue("currentContentControl", ""),
					m_configurationParameters.Attributes["id"].Value);
			}
		}

		// Set to true when sufficiently initialized that it makes sense to persist changes to split position.
		private bool m_fOkToPersistSplit = false;

		/// <summary />
		protected override void OnSplitterMoved(object sender, SplitterEventArgs e)
		{
			if (InSplitterMovedMethod)
				return;

			base.OnSplitterMoved(sender, e);

			// Persist new position.
			if (PropertyTable != null && m_fOkToPersistSplit)
			{
				PropertyTable.SetProperty(SplitterDistancePropertyName, SplitterDistance, true, false);
			}
		}

		private void SetSplitterDistance()
		{
			int sizeOfSharedDimension = Orientation == Orientation.Vertical ? Width : Height;
			// If the default size is specified in the XML file, use that,
			// otherwise compute something reasonable.
			string defaultLoc = XmlUtils.GetOptionalAttributeValue(m_configurationParameters,
				"defaultFixedPaneSizePoints",
				"50%");
			int defaultLocation;

			// Find 'total', which will be the height or width,
			// depending on the orientation of the multi pane.
			bool proportional = defaultLoc.EndsWith("%");
			int total;
			Size size = Size;
			if (m_parentSizeHint.Width != 0 && !proportional)
				size = m_parentSizeHint;
			if (Orientation == Orientation.Vertical)
				total = size.Width;
			else
				total = size.Height;

			if (proportional)
			{
				string percentStr = defaultLoc.Substring(0, defaultLoc.Length - 1);
				int percent = Int32.Parse(percentStr);
				float loc = (total * (((float)percent) / 100));
				double locD = Math.Round(loc);
				defaultLocation = (int)locD;
			}
			else
			{
				defaultLocation = Int32.Parse(defaultLoc);
			}

			if (PropertyTable != null)
			{
				// NB GetIntProperty RECORDS the default as if it had really been set by the user.
				// This behavior is disastrous here, where if we haven't truly persisted something,
				// we want to stick to computing the percent whenever the parent resizes.
				// So, first see whether there is a value in the property table at all.
				defaultLocation = PropertyTable.GetValue(SplitterDistancePropertyName, defaultLocation);
			}
			if (defaultLocation < kCollapsedSize)
				defaultLocation = kCollapsedSize;

			if (SplitterDistance != defaultLocation)
			{
				int originalSD = SplitterDistance;
				try
				{
					// Msg: SplitterDistance (aka: defaultLocation) must be between Panel1MinSize and Width - Panel2MinSize.
					if (defaultLocation >= Panel1MinSize && defaultLocation <= (sizeOfSharedDimension - Panel2MinSize))
					{
						// We do NOT want to persist this computed position!
						bool old = m_fOkToPersistSplit;
						m_fOkToPersistSplit = false;
						SplitterDistance = defaultLocation;
						m_fOkToPersistSplit = old;
					}
				}
				catch (Exception err)
				{
					Debug.WriteLine(err.Message);
					string msg = string.Format("Orientation: {0} Width: {1} Height: {2} Original SD: {3} New SD: {4} Panel1MinSize: {5} Panel2MinSize: {6} ID: {7} Panel1Collapsed: {8} Panel2Collapsed: {9}",
						Orientation.ToString(), Width, Height, originalSD, defaultLocation,
						Panel1MinSize, Panel2MinSize,
						XmlUtils.GetAttributeValue(m_configurationParameters, "id", "NOID"),
						Panel1Collapsed, Panel2Collapsed);
					throw new ArgumentOutOfRangeException(msg, err);
				}
			}
		}


#if RANDYTO
		/// summary>
		/// Receives the broadcast message "PropertyChanged." If it is the ShowFirstPane
		/// property, adjust.
		/// /summary>
		public void OnPropertyChanged(string name)
		{
			CheckDisposed();
			if (PropertyTable.GetValue("ToolForAreaNamed_lexicon", "") != toolName)
			{
				return;
			}
			if (name == "ActiveClerkSelectedObject" || name == "ToolForAreaNamed_lexicon")
			{
				SetFocusInDefaultControl();
			}
			if (name == m_propertyControllingVisibilityOfFirstPane)
			{
				bool fShowFirstPane = PropertyTable.GetValue(m_propertyControllingVisibilityOfFirstPane, true);
				if (fShowFirstPane == m_showingFirstPane)
					return; // just in case it didn't really change

				m_showingFirstPane = fShowFirstPane;

				Panel1Collapsed = !fShowFirstPane;

				if (ShowFirstPaneChanged != null)
					ShowFirstPaneChanged(this, new EventArgs());
			}

		}
		/// <summary>
		/// The focus will only be set in the default control if it implements IFocusablePanePortion.
		/// Note that it may BE our First or SecondPane, or it may be a child of one of those.
		/// </summary>
		private void SetFocusInDefaultControl()
		{
			if (String.IsNullOrEmpty(m_defaultFocusControl))
				return;
			var defaultFocusControl = (IFwMainWnd.FindControl(FirstControl, m_defaultFocusControl) ??
				IFwMainWnd.FindControl(SecondControl, m_defaultFocusControl)) as IFocusablePanePortion;
			Debug.Assert(defaultFocusControl != null,
				"Failed to find focusable subcontrol.",
				"This MultiPane was configured to focus {0} as a default control. But it either was not found or was not an IFocuablePanePortion",
				m_defaultFocusControl);
			// LT-14222...can't do BeginInvoke until our handle is created...we attempt this multiple times since it is hard
			// to find the right time to do it. If we can't do it yet hope we can do it later.
			if (defaultFocusControl != null && this.IsHandleCreated)
			{
				defaultFocusControl.IsFocusedPane = true; // Lets it know it can do any special behavior (e.g., DataPane) when it is the focused child.
				BeginInvoke((MethodInvoker) (() => defaultFocusControl.Focus()));
			}
		}
#endif

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
		/// <param name="propertyTable">Interface to a property table.</param>
		/// <param name="publisher">Interface to the publisher.</param>
		/// <param name="subscriber">Interface to the subscriber.</param>
		public void InitializeFlexComponent(IPropertyTable propertyTable, IPublisher publisher, ISubscriber subscriber)
		{
			FlexComponentCheckingService.CheckInitializationValues(propertyTable, publisher, subscriber, PropertyTable, Publisher, Subscriber);

			PropertyTable = propertyTable;
			Publisher = publisher;
			Subscriber = subscriber;

			m_showingFirstPane = false; // Default to not showing.
		}

		#endregion

		#region Implementation of IMainUserControl

		/// <summary>
		/// Get or set the name to be used by the accessibility object.
		/// </summary>
		string IMainUserControl.AccName { get; set; }

		#endregion
	}

	/// <summary>
	/// Temporarily set up and take down a tool.
	/// </summary>
	internal static class TemporaryToolProviderHack
	{
		/// <summary />
		internal static void SetupToolDisplay(ICollapsingSplitContainer mainCollapsingSplitContainer, ITool tool)
		{
			mainCollapsingSplitContainer.SecondControl.SuspendLayout();
			var newTempControl = new Label
			{
				Text = @"Selected Tool: " + tool.UiName,
				Dock = DockStyle.Fill,
				ForeColor = Color.Ivory,
				BackColor = Color.Coral
			};
			mainCollapsingSplitContainer.SecondControl.Controls.Add(newTempControl);
			mainCollapsingSplitContainer.SecondControl.ResumeLayout();
		}

		/// <summary />
		internal static void RemoveToolDisplay(ICollapsingSplitContainer mainCollapsingSplitContainer)
		{
			mainCollapsingSplitContainer.SecondControl.SuspendLayout();
			if (mainCollapsingSplitContainer.SecondControl.Controls.Count == 0)
			{
				return;
			}
			var tempControl = mainCollapsingSplitContainer.SecondControl.Controls[0];
			try
			{
				mainCollapsingSplitContainer.SecondControl.Controls.RemoveAt(0);
			}
			finally
			{
				tempControl.Dispose();
			}
			mainCollapsingSplitContainer.SecondControl.ResumeLayout();
		}
	}
}
