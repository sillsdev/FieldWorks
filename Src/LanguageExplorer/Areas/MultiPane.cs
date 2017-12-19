// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using LanguageExplorer.Controls;
using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorer.Areas
{
#if RANDYTODO
	// TODO: Why do we need "CollapsingSplitContainer" & "MultiPane"? Can they be collapsed into one?
	// TODO: "CollapsingSplitContainer" is used by the main window as its top level splitter,
	// TODO: and it is the right pane of main CollapsingSplitContainer instance for tools with a RecordBar.
	// TODO: "MultiPane" is then used by numerous tools in the right half of that main or second "CollapsingSplitContainer".
#endif
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
	internal class MultiPane : CollapsingSplitContainer, IMainContentControl
	{
		private readonly string m_areaMachineName;
		private readonly string m_id;
		// When its superclass gets switched to the new SplitContainer class. it has to implement IMainUserControl itself.
		private IContainer components;
		private Size m_parentSizeHint;
		private string m_defaultPrintPaneId;
		private readonly string m_defaultFocusControl;
		private readonly string m_defaultFixedPaneSizePoints;
		private readonly string m_persistContext;
		private readonly string m_label;
		// Set to true when sufficiently initialized that it makes sense to persist changes to split position.
		private bool m_fOkToPersistSplit;

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
		internal MultiPane(MultiPaneParameters parameters)
			: this()
		{
			m_areaMachineName = parameters.Area.MachineName;
			m_id = parameters.Id ?? "NOID";
			m_defaultFixedPaneSizePoints = parameters.DefaultFixedPaneSizePoints ?? "50%";
			m_defaultPrintPaneId = parameters.DefaultPrintPane ?? string.Empty;
			m_defaultFocusControl = parameters.DefaultFocusControl ?? string.Empty;
			m_persistContext = parameters.PersistContext;
			m_label = parameters.Label;

			Orientation = parameters.Orientation;
			Dock = DockStyle.Fill;
			SplitterWidth = 5;

			FirstControl = parameters.FirstControlParameters.Control;
			FirstLabel = parameters.FirstControlParameters.Label;
			FirstControl.Dock = DockStyle.Fill;

			SecondControl = parameters.SecondControlParameters.Control;
			SecondLabel = parameters.SecondControlParameters.Label;
			SecondControl.Dock = DockStyle.Fill;

			// Has to be done later for clients that know about "m_defaultFocusControl".
			SecondCollapseZone = parameters.SecondCollapseZone;
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
			//Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
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

				var name = m_persistContext;
				if (string.IsNullOrEmpty(name))
				{
					name = m_id;
				}
				if (string.IsNullOrEmpty(name))
				{
					name = m_label;
				}
				if (string.IsNullOrEmpty(name))
				{
					name = "MultiPane";
				}

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

				return m_areaMachineName;
			}
		}

		#endregion // IMainContentControl implementation

		#region ICtrlTabProvider implementation

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

		private string SplitterDistancePropertyName => $"MultiPaneSplitterDistance_{m_areaMachineName}_{PropertyTable.GetValue<string>(AreaServices.ToolChoice)}_{m_id}";

		/// <summary />
		protected override void OnSplitterMoved(object sender, SplitterEventArgs e)
		{
			if (InSplitterMovedMethod)
				return;

			base.OnSplitterMoved(sender, e);

			// Persist new position.
			if (m_fOkToPersistSplit)
			{
				PropertyTable.SetProperty(SplitterDistancePropertyName, SplitterDistance, true, false);
			}
		}

		/// <param name="e">An <see cref="T:System.EventArgs" /> that contains the event data. </param>
		protected override void OnParentChanged(EventArgs e)
		{
			base.OnParentChanged(e);
			if (Parent != null)
			{
				ResetSplitterEventHandler(true);
			}
		}

		private void SetSplitterDistance()
		{
			int sizeOfSharedDimension = Orientation == Orientation.Vertical ? Width : Height;
			int defaultLocation;

			// Find 'total', which will be the height or width,
			// depending on the orientation of the multi pane.
			bool proportional = m_defaultFixedPaneSizePoints.EndsWith("%");
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
				string percentStr = m_defaultFixedPaneSizePoints.Substring(0, m_defaultFixedPaneSizePoints.Length - 1);
				int percent = Int32.Parse(percentStr);
				float loc = (total * (((float)percent) / 100));
				double locD = Math.Round(loc);
				defaultLocation = (int)locD;
			}
			else
			{
				defaultLocation = Int32.Parse(m_defaultFixedPaneSizePoints);
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
						Orientation, Width, Height, originalSD, defaultLocation,
						Panel1MinSize, Panel2MinSize,
						m_id,
						Panel1Collapsed, Panel2Collapsed);
					throw new ArgumentOutOfRangeException(msg, err);
				}
			}
		}

		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);

			SetFocusInDefaultControl();
		}

		/// <summary>
		/// The focus will only be set in the default control if it implements IFocusablePanePortion.
		/// Note that it may BE our First or SecondPane, or it may be a child of one of those.
		/// </summary>
		private void SetFocusInDefaultControl()
		{
			if (String.IsNullOrEmpty(m_defaultFocusControl))
				return;
			var defaultFocusControl = (FwUtils.FindControl(FirstControl, m_defaultFocusControl) ??
				FwUtils.FindControl(SecondControl, m_defaultFocusControl)) as IFocusablePanePortion;
			Debug.Assert(defaultFocusControl != null,
				"Failed to find focusable subcontrol.",
				"This MultiPane was configured to focus {0} as a default control. But it either was not found or was not an IFocuablePanePortion",
				m_defaultFocusControl);
			// LT-14222...can't do BeginInvoke until our handle is created...we attempt this multiple times since it is hard
			// to find the right time to do it. If we can't do it yet hope we can do it later.
			if (defaultFocusControl != null && IsHandleCreated)
			{
				defaultFocusControl.IsFocusedPane = true; // Lets it know it can do any special behavior (e.g., DataPane) when it is the focused child.
				BeginInvoke((MethodInvoker) (() => defaultFocusControl.Focus()));
			}
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

			Panel1Collapsed = !PropertyTable.GetValue(string.Format("Show_{0}", m_id), true);

			m_fOkToPersistSplit = true;
			SetSplitterDistance();
		}

		#endregion

		#region Implementation of IMainUserControl

		/// <summary>
		/// Get or set the name to be used by the accessibility object.
		/// </summary>
		string IMainUserControl.AccName { get; set; }

		#endregion
	}
}
