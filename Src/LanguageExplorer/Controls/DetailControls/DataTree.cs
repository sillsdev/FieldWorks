// Copyright (c) 2005-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Xml.XPath;
using LanguageExplorer.Areas;
using LanguageExplorer.Controls.XMLViews;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using SIL.LCModel.Utils;
using SIL.PlatformUtilities;
using SIL.Xml;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	/// A DataTree displays a tree diagram alongside a collection of controls. Each control is
	/// represented as a Slice, and typically contains an actual .NET control of some
	/// sort (most often, in FieldWorks, a subclass of SIL.FieldWorks.Common.Framework.RootSite).
	/// The controls are arranged vertically, one under the other, and the tree diagram is
	/// aligned with the controls.
	///
	/// The creator of a DataTree is responsible to add items to it, though DataTree
	/// provide helpful methods for adding
	/// certain commonly useful controls. Additional items may be added as a result of user
	/// actions, typically expanding and contracting nodes.
	///
	/// Much of the standard behavior of the DataTree is achieved by delegating it to virtual
	/// methods of Slice, which can be subclassed to specialize this behavior.
	///
	/// Review JohnT: do I have the right superclass? This choice allows the window to have
	/// a scroll bar and to contain other controls, and seems to be the intended superclass
	/// for stuff developed by application programmers.
	/// </summary>
	/// Possible superclasses for DataTree:
	/// System.Windows.Forms.Panel
	/// System.Windows.Forms.ContainerControl
	/// System.Windows.Forms.UserControl
	internal class DataTree : UserControl, IVwNotifyChange, IFlexComponent, IRefreshableRoot
	{
		/// <summary>
		/// Occurs when the current slice changes
		/// </summary>
		internal event CurrentSliceChangedEventHandler CurrentSliceChanged;

		internal DataTreeSliceContextMenuParameterObject DataTreeSliceContextMenuParameterObject { get; private set; }

		#region Data members

		/// <summary>
		/// Use this to do the Add/RemoveNotifications, since it can be used in the unmanaged section of Dispose.
		/// (If m_sda is COM, that is.)
		/// Doing it there will be safer, since there was a risk of it not being removed
		/// in the managed section, as when disposing was done by the Finalizer.
		/// </summary>
		private ISilDataAccess m_sda;
		/// <summary>allows us to interpret class and field names and trace superclasses.</summary>
		protected IFwMetaDataCache m_mdc;
		/// <summary />
		protected Slice m_currentSlice;
		/// <summary>used to restore current slice during RefreshList()</summary>
		private string m_currentSlicePartName;
		/// <summary>used to restore current slice during RefreshList()</summary>
		private Guid m_currentSliceObjGuid = Guid.Empty;
		/// <summary>used to restore current slice during RefreshList()</summary>
		private bool m_fSetCurrentSliceNew;
		/// <summary>used to restore current slice during RefreshList()</summary>
		private Slice m_currentSliceNew;
		/// <summary>used to restore current slice during RefreshList()</summary>
		private string m_sPartNameProperty;
		/// <summary>used to restore current slice during RefreshList()</summary>
		private string m_sObjGuidProperty;
		/// <summary />
		protected string m_layoutChoiceField;
		/// <summary>This is the position a splitter would be if we had a single one, the actual
		/// position of the splitter in a zero-indent slice. This is persisted, and can also be
		/// controlled by the XML file; the value here is a last-resort default.
		/// </summary>
		protected int m_sliceSplitPositionBase = 150;
		/// <summary>inventory of layouts for different object classes.</summary>
		protected Inventory m_layoutInventory;
		/// <summary>inventory of parts used in layouts.</summary>
		protected Inventory m_partInventory;
		/// <summary />
		protected internal bool m_fHasSplitter;
		/// <summary>Set of KeyValuePair objects (hvo, flid), properties for which we must refresh if altered.</summary>
		protected HashSet<Tuple<int, int>> m_monitoredProps = new HashSet<Tuple<int, int>>();
		/// <summary>Number of times DeepSuspendLayout has been called without matching DeepResumeLayout.</summary>
		private int m_cDeepSuspendLayoutCount;
		private ISharedEventHandlers _sharedEventHandlers;
		protected LcmStyleSheet m_styleSheet;
		/// <summary>
		/// used for slice tree nodes. All tooltips are cleared when we switch records!
		/// </summary>
		protected ToolTip m_tooltip;
		protected LayoutStates m_layoutState = LayoutStates.klsNormal;
		/// <summary>
		/// width of right pane (if any) the last time we did a layout.
		/// </summary>
		protected int m_dxpLastRightPaneWidth = -1;
		protected IRecordChangeHandler m_rch;
		protected IRecordListUpdater m_rlu;
		protected string m_listName;
		private bool m_fDisposing;
		/// <summary>
		/// this helps DataTree delay from setting focus in a slice, until we're all setup to do so.
		/// </summary>
		private bool m_fSuspendSettingCurrentSlice;
		private bool m_fCurrentContentControlObjectTriggered;
		/// <summary>
		/// These variables are used to prevent refreshes from occurring when they're not wanted,
		/// but then to do a refresh when it's safe.
		/// </summary>
		private bool m_fDoNotRefresh;
		private bool m_fPostponedClearAllSlices;

		// Set during ConstructSlices, to suppress certain behaviors not safe at this point.
		internal bool ConstructingSlices { get; private set; }
		public List<Slice> Slices { get; }

		#endregion Data members

		#region constants

		/// <summary />
		public const int HeavyweightRuleThickness = 2;
		/// <summary />
		public const int HeavyweightRuleAboveMargin = 10;

		#endregion constants

		#region TraceSwitch methods
		/// <summary>
		/// Control how much output we send to the application's listeners (e.g. visual studio output window)
		/// </summary>
		protected TraceSwitch m_traceSwitch = new TraceSwitch(LanguageExplorerConstants.DataTree, string.Empty);

		protected void TraceVerbose(string s)
		{
			if (m_traceSwitch.TraceVerbose)
			{
				Trace.Write(s);
			}
		}
		protected void TraceVerboseLine(string s)
		{
			if (m_traceSwitch.TraceVerbose)
			{
				Trace.WriteLine("DataTreeThreadID=" + System.Threading.Thread.CurrentThread.GetHashCode() + ": " + s);
			}
		}
		protected void TraceInfoLine(string s)
		{
			if (m_traceSwitch.TraceInfo || m_traceSwitch.TraceVerbose)
			{
				Trace.WriteLine("DataTreeThreadID=" + System.Threading.Thread.CurrentThread.GetHashCode() + ": " + s);
			}
		}
		#endregion

		#region Slice collection manipulation methods

		private ToolTip ToolTip => m_tooltip ?? (m_tooltip = new ToolTip { ShowAlways = true });

		private void InsertSliceAndRegisterWithContextHelp(int index, Slice slice)
		{
			slice.RegisterWithContextHelper();
			InsertSlice(index, slice);
		}

		private void InsertSlice(int index, Slice slice)
		{
			InstallSlice(slice, index);
			ResetTabIndices(index);
			if (!m_fSetCurrentSliceNew || slice.IsHeaderNode)
			{
				return;
			}
			m_fSetCurrentSliceNew = false;
			if (m_currentSliceNew == null || m_currentSliceNew.IsDisposed)
			{
				m_currentSliceNew = slice;
			}
		}

		private void InstallSlice(Slice slice, int index)
		{
			Debug.Assert(index >= 0 && index <= Slices.Count);

			slice.SuspendLayout();
			slice.Install(this);
			ForceSliceIndex(slice, index);
			Debug.Assert(slice.IndexInContainer == index, $"InstallSlice: slice '{(slice.ConfigurationNode != null && slice.ConfigurationNode.GetOuterXml() != null ? slice.ConfigurationNode.GetOuterXml() : "(DummySlice?)")}' at index({slice.IndexInContainer}) should have been inserted in index({index}).");
			// Note that it is absolutely vital to do this AFTER adding the slice to the data tree.
			// Otherwise, the tooltip appears behind the form and is usually never seen.
			SetToolTip(slice);
			slice.ResumeLayout();
			// Make sure it isn't added twice.
			AdjustSliceSplitPosition(slice);
		}

		/// <summary>
		/// For some strange reason, the first Controls.SetChildIndex doesn't always put it in the specified index.
		/// The second time seems to work okay though.
		/// </summary>
		private void ForceSliceIndex(Slice slice, int index)
		{
			if (index >= Slices.Count || Slices[index] == slice)
			{
				return;
			}
			Slices.Remove(slice);
			Slices.Insert(index, slice);
		}

		private void SetToolTip(Slice slice)
		{
			if (slice.ToolTip != null)
			{
				ToolTip.SetToolTip(slice.TreeNode, slice.ToolTip);
			}
		}

		private void slice_SplitterMoved(object sender, SplitterEventArgs e)
		{
			if (m_currentSlice == null)
			{
				return; // Too early to do much;
			}
			var movedSlice = sender is Slice ? (Slice)sender : (Slice)((SplitContainer)sender).Parent; // Have to move up one parent notch to get to the Slice.
			if (m_currentSlice != movedSlice)
			{
				return; // Too early to do much;
			}
			Debug.Assert(movedSlice == m_currentSlice);
			m_sliceSplitPositionBase = movedSlice.SplitCont.SplitterDistance - movedSlice.LabelIndent();
			PersistPreferences();
			SuspendLayout();
			foreach (var otherSlice in Slices)
			{
				if (movedSlice != otherSlice)
				{
					AdjustSliceSplitPosition(otherSlice);
				}
			}
			ResumeLayout(false);
			// This can affect the lines between the slices. We need to redraw them but not the
			// slices themselves.
			Invalidate(false);
			movedSlice.TakeFocus(true);
		}

		private void AdjustSliceSplitPosition(Slice otherSlice)
		{
			var otherSliceSC = otherSlice.SplitCont;
			// Remove and re-add event handler when setting the value for the other fellow.
			otherSliceSC.SplitterMoved -= slice_SplitterMoved;
			otherSlice.SetSplitPosition();
			otherSliceSC.SplitterMoved += slice_SplitterMoved;
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);
			foreach (var slice in Slices)
			{
				AdjustSliceSplitPosition(slice);
			}
		}

		protected void InsertSliceRange(int insertPosition, ISet<Slice> slices)
		{
			var indexableSlices = new List<Slice>(slices.ToArray());
			for (var i = indexableSlices.Count - 1; i >= 0; --i)
			{
				InstallSlice(indexableSlices[i], insertPosition);
			}
			ResetTabIndices(insertPosition);
		}

		/// <summary>
		/// Use with care...if it's a real slice, or a real one being replaced, there are
		/// other things to do like adding to or removing from container. This is mainly for messing
		/// with dummy slices.
		/// </summary>
		internal void RawSetSlice(int index, Slice slice)
		{
			Debug.Assert(slice != Slices[index], "Can't replace the slice with itself.");

			RemoveSliceAt(index);
			InstallSlice(slice, index);
			SetTabIndex(index);
		}

		internal void RemoveSliceAt(int index)
		{
			RemoveSlice(Slices[index], index);
		}

		/// <summary>
		/// Removes a slice but does NOT clean up tooltips; caller should do that.
		/// </summary>
		private void RemoveSlice(Slice gonner)
		{
			RemoveSlice(gonner, Slices.IndexOf(gonner), false);
		}

		/// <summary>
		/// this should ONLY be called from slice.Dispose(). It makes sure that when a slice
		/// is removed by disposing it directly it gets removed from the Slices collection.
		/// </summary>
		internal void RemoveDisposedSlice(Slice gonner)
		{
			Slices.Remove(gonner);
		}

		private void RemoveSlice(Slice gonner, int index, bool fixToolTips = true)
		{
			gonner.AboutToDiscard();
			gonner.SplitCont.SplitterMoved -= slice_SplitterMoved;
			Controls.Remove(gonner);
			Debug.Assert(Slices[index] == gonner);
			Slices.RemoveAt(index);
			// Reset CurrentSlice, if appropriate.
			if (gonner == m_currentSlice)
			{
				Slice newCurrent = null;
				if (Slices.Count > index)
				{
					// Get the one at the same index (next one after the one being removed).
					newCurrent = Slices[index];
				}
				else if (Slices.Count > 0 && Slices.Count > index - 1)
				{
					// Get the one before index.
					newCurrent = Slices[index - 1];
				}
				if (newCurrent != null)
				{
					CurrentSlice = newCurrent;
				}
				else
				{
					m_currentSlice = null;
					gonner.SetCurrentState(false);
				}
			}
			// Since "gonner's" SliceTreeNode still is referenced by m_tooltip,
			// (if it has one at all, that is),
			// we have to also remove with ToolTip for gonner,
			// Since the dumb MS ToolTip class can't just remove one,
			// we have to remove them all and re-add the remaining ones
			// in order to have it really turn loose of the SliceTreeNode.
			// But, only do all of that, if it actually has a ToolTip.
			var gonnerHasToolTip = fixToolTips && (gonner.ToolTip != null);
			if (gonnerHasToolTip)
			{
				m_tooltip.RemoveAll();
			}
			gonner.Dispose();
			// Now, we need to re-add all of the surviving tooltips.
			if (gonnerHasToolTip)
			{
				foreach (var keeper in Slices)
				{
					SetToolTip(keeper);
				}
			}
			ResetTabIndices(index);
		}

		private void SetTabIndex(int index)
		{
			var slice = Slices[index];
			if (slice.IsRealSlice)
			{
				slice.TabIndex = index;
				slice.TabStop = slice.Control != null && slice.Control.TabStop;
			}
		}

		/// <summary>
		/// Resets the TabIndex for all slices that are located at, or above, the <c>startingIndex</c>.
		/// </summary>
		/// <param name="startingIndex">The index to start renumbering the TabIndex.</param>
		private void ResetTabIndices(int startingIndex)
		{
			for (var i = startingIndex; i < Slices.Count; ++i)
			{
				SetTabIndex(i);
			}
		}

		#endregion Slice collection manipulation methods

		internal DataTree(ISharedEventHandlers sharedEventHandlers, bool showingAllFields)
		{
			Guard.AgainstNull(sharedEventHandlers, nameof(sharedEventHandlers));

			ShowingAllFields = showingAllFields;
			DataTreeSliceContextMenuParameterObject = new DataTreeSliceContextMenuParameterObject();
			Slices = new List<Slice>();
			_sharedEventHandlers = sharedEventHandlers;
		}

		/// <summary>
		/// Get the current slice as StTextSlice, or null if the is no current slice or it isn't a StTextSlice.
		/// </summary>
		internal StTextSlice CurrentSliceAsStTextSlice => CurrentSlice as StTextSlice;

		/// <summary>
		/// Get the root layout name.
		/// </summary>
		public string RootLayoutName { get; protected set; } = "default";

		/// <summary>
		/// Get/Set a stylesheet suitable for use in views.
		/// Ideally, there should be just one for the whole application, so if the app has
		/// more than one datatree, do something to ensure this.
		/// Also, something external should set it if the default stylesheet (the styles
		/// stored in LangProject.Styles) is not to be used.
		/// Otherwise, the datatree will automatically load it when first requested
		/// (provided it has a cache by that time).
		/// </summary>
		public LcmStyleSheet StyleSheet
		{
			get
			{
				if (m_styleSheet == null && Cache != null)
				{
					m_styleSheet = new LcmStyleSheet();
					m_styleSheet.Init(Cache, Cache.LanguageProject.Hvo, LangProjectTags.kflidStyles);
				}
				return m_styleSheet;
			}

			set
			{
				m_styleSheet = value;
			}

		}

		public void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			if (m_monitoredProps.Contains(Tuple.Create(hvo, tag)))
			{
				RefreshList(false);
				OnFocusFirstPossibleSlice(null);
			}
			// Note, in LinguaLinks import we don't have an action handler when we hit this.
			else if (Cache.DomainDataByFlid.GetActionHandler() != null && Cache.DomainDataByFlid.GetActionHandler().IsUndoOrRedoInProgress)
			{
				// Redoing an Add or Undoing a Delete may not have an existing slice to work with, so just force
				// a list refresh.  See LT-6033.
				if (Root != null && hvo == Root.Hvo)
				{
					var type = (CellarPropertyType)m_mdc.GetFieldType(tag);
					if (type == CellarPropertyType.OwningCollection || type == CellarPropertyType.OwningSequence || type == CellarPropertyType.ReferenceCollection || type == CellarPropertyType.ReferenceSequence)
					{
						RefreshList(true);
						// Try to make sure some slice ends up current.
						OnFocusFirstPossibleSlice(null);
						return;
					}
				}
				// some FieldSlices (e.g. combo slices)may want to Update their display
				// if its field changes during an Undo/Redo (cf. LT-4861).
				RefreshList(hvo, tag);
			}
		}

		/// <summary>
		/// Tells whether we are showing all fields, or just the ones requested.
		/// </summary>
		public bool ShowingAllFields { get; protected set; }

		/// <summary>
		/// Return the slice which should receive commands.
		/// NB: This may be null.
		/// </summary>
		/// <remarks>
		/// Originally, I had called this FocusSlice, but that was misleading because
		/// some slices do not have any control, or have one but it cannot be focused upon.
		/// currently, you get to be the current slice if
		/// 1) your control receives focus
		/// 2) the user clicks on your tree control
		/// </remarks>
		/// <exception cref="ArgumentException">Thrown if trying to set the current slice to null.</exception>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Slice CurrentSlice
		{
			get
			{
				// Trap this before the throw to give the debugger a chance to analyze.
				Debug.Assert(m_currentSlice == null || !m_currentSlice.IsDisposed, "CurrentSlice is already disposed??");
				return m_currentSlice;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentException("CurrentSlice on DataTree cannot be set to null. Set the underlying data member to null, if you really want it to be null.");
				}
				Debug.Assert(!value.IsDisposed, "Setting CurrentSlice to a disposed slice -- not a good idea!");
				// don't set the current slice until we're all setup to do so (LT-7307)
				if (m_currentSlice == value || m_fSuspendSettingCurrentSlice)
				{
					// LT-17633 But if we are trying to set a different slice from the one planned,
					// we need to remember that. This can happen, for instance, when we insert several
					// slices to replace a ghost field, but we want the current slice to be other than
					// the first one.
					m_currentSliceNew = value;
					return;
				}
				Slice oldSlice = null;
				// Tell the old geezer it isn't current anymore.
				if (m_currentSlice != null)
				{
					m_currentSlice.Validate();
					if (m_currentSlice.Control is ContainerControl)
					{
						((ContainerControl)m_currentSlice.Control).Validate();
					}
					m_currentSlice.SetCurrentState(false);
					oldSlice = m_currentSlice;
				}
				m_currentSlice = value;
				// Tell new guy it is current.
				m_currentSlice.SetCurrentState(true);
				var index = m_currentSlice.IndexInContainer;
				ScrollControlIntoView(m_currentSlice);
				// Ensure that we can tab and shift-tab. This requires that at least one
				// following and one prior slice be a tab stop, if possible.
				for (var i = index + 1; i < Slices.Count; i++)
				{
					MakeSliceRealAt(i);
					if (Slices[i].TabStop)
					{
						break;
					}
				}
				for (var i = index - 1; i >= 0; i--)
				{
					MakeSliceRealAt(i);
					if (Slices[i].TabStop)
					{
						break;
					}
				}
				Invalidate();
				// update the current descendant
				Descendant = DescendantForSlice(m_currentSlice);
				CurrentSliceChanged?.Invoke(this, new CurrentSliceChangedEventArgs(oldSlice, m_currentSlice));
			}
		}

		/// <summary>
		/// Get the object that is considered the descendant for the given slice, that is,
		/// the object of a header node which is one of the slice's parents (or the slice itself),
		/// if possible, otherwise, the root object. May return null if the nearest Parent is disposed.
		/// </summary>
		private ICmObject DescendantForSlice(Slice slice)
		{
			var loopSlice = slice;
			while (loopSlice != null && !loopSlice.IsDisposed)
			{
				// if there is not parent slice, we must be on a root slice
				if (loopSlice.ParentSlice == null)
				{
					return Root;
				}
				// if we are on a header slice, the slice's object is the descendant
				if (loopSlice.IsHeaderNode)
				{
					return loopSlice.MyCmObject.IsValidObject ? loopSlice.MyCmObject : null;
				}
				loopSlice = loopSlice.ParentSlice;
			}
			// The following (along with the Disposed check above) prevents
			// LT-11455 Crash if we delete the last compound rule.
			// And LT-11463 Crash if Lexeme Form is filtered to Blanks.
			return Root.IsValidObject ? Root : null;
		}

		/// <summary>
		/// Determines whether the containing tree has a SubPossibilities slice.
		/// </summary>
		public bool HasSubPossibilitiesSlice
		{
			get
			{
				// Start at the end of the list, since we usually put SubPossibilities there.
				var i = Slices.Count - 1;
				for (; i >= 0; --i)
				{
					var current = Slices[i];
					// Not sure these two cases are general enough to find a SubPossibilities slice.
					// Ideally we want to find the <seq field='SubPossibilities'> node, but in the case of
					// a header node, it's not that easy, and I (EricP) am not sure how to actually get from the
					// <part ref='SubPossibilities'> down to the seq in that case. Works for now!
					if (current.IsSequenceNode)
					{
						// see if slices is a SubPossibilities
						var node = current.ConfigurationNode.Element("seq");
						var field = XmlUtils.GetOptionalAttributeValue(node, "field");
						if (field == "SubPossibilities")
						{
							break;
						}
					}
					else if (current.IsHeaderNode)
					{
						var node = current.CallerNode.XPathSelectElement("*/part[starts-with(@ref,'SubPossibilities')]");
						if (node != null)
						{
							break;
						}
					}
				}
				// if we found a SubPossibilities slice, the index will be in range.
				return i >= 0 && i < Slices.Count;
			}
		}

		public ICmObject Root { get; protected set; }

		public ICmObject Descendant { get; protected set; }

		public void Reset()
		{
			var savedLayoutState = m_layoutState;
			m_layoutState = LayoutStates.klsClearingAll;
			try
			{
				// Get rid of all the slices...makes sure none of them can keep focus (e.g., see LT-11348)
				var slices = Slices.ToArray();
				foreach (var slice in slices) //inform all the slices they are about to be discarded, remove the trees handler from them
				{
					slice.AboutToDiscard();
					slice.SplitCont.SplitterMoved -= slice_SplitterMoved;
				}
				Controls.Clear(); //clear the controls
				Slices.Clear(); //empty the slices collection
				foreach (var slice in slices) //make sure the slices don't think they are active, dispose them
				{
					slice.SetCurrentState(false);
					slice.Dispose();
				}
				//no more current slice
				m_currentSlice = null;
				// A tooltip doesn't always exist: see LT-11441, LT-11442, and LT-11444.
				m_tooltip?.RemoveAll();
				Root = null;
			}
			finally
			{
				m_layoutState = savedLayoutState;
			}
		}

		private void ResetRecordListUpdater()
		{
			if (m_listName == null || m_rlu != null)
			{
				return;
			}
			// Find the first parent IRecordListOwner object (if any) that
			// owns an IRecordListUpdater.
			var rlo = PropertyTable.GetValue<IRecordListOwner>(FwUtils.window);
			if (rlo != null)
			{
				m_rlu = rlo.FindRecordListUpdater(m_listName);
			}
		}

		/// <summary>
		/// Set the base split position of the DataTree and all slices.
		/// </summary>
		/// <remarks>
		/// Note: This value is a base value and should never include the LabelIndent offset.
		/// Each Slice will add its own Label length, when its SplitterDistance is set.
		/// </remarks>
		public int SliceSplitPositionBase
		{
			get
			{
				return m_sliceSplitPositionBase;
			}
			set
			{
				if (value == m_sliceSplitPositionBase)
				{
					return;
				}
				m_sliceSplitPositionBase = value;
				PersistPreferences();
				SuspendLayout();
				foreach (var slice in Slices)
				{
					var sc = slice.SplitCont;
					sc.SplitterMoved -= slice_SplitterMoved;
					slice.SetSplitPosition();
					sc.SplitterMoved += slice_SplitterMoved;
				}
				ResumeLayout(false);
				// This can affect the lines between the slices. We need to redraw them but not the
				// slices themselves.
				Invalidate(false);
			}
		}

		/// <summary>
		/// a look up table for getting the correct version of strings that the user will see.
		/// </summary>
		public SliceFilter SliceFilter { get; set; }

		public IPersistenceProvider PersistenceProvder { get; set; }

		private void MonoIgnoreUpdates()
		{
#if USEIGNOREUPDATES
			// static method call to get reasonable performance from mono
			// IgnoreUpdates is custom functionality added to mono's winforms
			// (commit 3233f63ece7e922d03a115ce8d8524e8554be19d)

			// Stops all winforms Size events
			Control.IgnoreUpdates();
#endif
		}

		private void MonoResumeUpdates()
		{
#if USEIGNOREUPDATES
			// static method call to get reasonable performance from mono
			// Resumes all winforms Size events
			Control.UnignoreUpdates();
#endif
		}

		/// <summary>
		/// Shows the specified object and makes the slices for the descendant object visible.
		/// </summary>
		public virtual void ShowObject(ICmObject root, string layoutName, string layoutChoiceField, ICmObject descendant, bool suppressFocusChange)
		{
			if (Root == root && layoutName == RootLayoutName && layoutChoiceField == m_layoutChoiceField && Descendant == descendant)
			{
				return;
			}
			// Initialize our internal state with the state of the PropertyTable
			SetCurrentSlicePropertyNames();
			m_currentSlicePartName = PropertyTable.GetValue<string>(m_sPartNameProperty, SettingsGroup.LocalSettings);
			m_currentSliceObjGuid = PropertyTable.GetValue(m_sObjGuidProperty, Guid.Empty, SettingsGroup.LocalSettings);
			PropertyTable.SetProperty(m_sPartNameProperty, null, true, settingsGroup: SettingsGroup.LocalSettings);
			PropertyTable.SetProperty(m_sObjGuidProperty, Guid.Empty, true, settingsGroup: SettingsGroup.LocalSettings);
			m_currentSliceNew = null;
			m_fSetCurrentSliceNew = false;
			MonoIgnoreUpdates();
			using (new DataTreeLayoutSuspensionHelper(PropertyTable.GetValue<IFwMainWnd>(FwUtils.window), this))
			{
				try
				{
					RootLayoutName = layoutName;
					m_layoutChoiceField = layoutChoiceField;
					Debug.Assert(Cache != null, "You need to call Initialize() first.");
					if (Root != root)
					{
						Root = root;
						if (m_rch != null)
						{
							// We need to refresh the record list if homograph numbers change.
							// Do it for the old object.
							m_rch.Fixup(true);
							// Root has changed, so reset the handler.
							m_rch.Setup(Root, m_rlu, Cache);
						}
						Invalidate(); // clears any lines left over behind slices.
						CreateSlices(true);
						if (root != descendant && (m_currentSliceNew == null || m_currentSliceNew.IsDisposed || m_currentSliceNew.MyCmObject != descendant))
						{
							// if there is no saved current slice, or it is for the wrong object, set the current slice to be the first non-header
							// slice of the descendant object
							SetCurrentSliceNewFromObject(descendant);
						}
					}
					else if (Descendant != descendant)
					{
						// we are on the same root, but different descendant
						if (root != descendant)
						{
							SetCurrentSliceNewFromObject(descendant);
						}
					}
					else
					{
						RefreshList(false);  // This could be optimized more, too, but it isn't the common case.
					}

					Descendant = descendant;
					// start new object at top (unless first focusable slice changes it).
					AutoScrollPosition = new Point(0, 0);
					// We can't focus yet because the data tree slices haven't finished displaying.
					// (Remember, Windows won't let us focus something that isn't visible.)
					// (See LT-3915.)  So postpone focusing by scheduling it to execute on idle...
					// allow OnReadyToSetCurrentSlice to focus first possible control.
					m_fCurrentContentControlObjectTriggered = true;
					// Tests have no IdleQueue.
					PropertyTable.GetValue<IFwMainWnd>(FwUtils.window).IdleQueue?.Add(IdleQueuePriority.High, OnReadyToSetCurrentSlice, suppressFocusChange);
					// prevent setting focus in slice until we're all setup (cf.
					m_fSuspendSettingCurrentSlice = true;
				}
				finally
				{
					MonoResumeUpdates();
					EnsureDefaultCursorForSlices();
				}
			}
		}

		internal void EnsureDefaultCursorForSlices()
		{
			foreach (var slice in Slices)
			{
				if (slice.Cursor == Cursors.WaitCursor)
				{
					slice.Cursor = Cursors.Default;
				}
			}
		}

		private void SetCurrentSliceNewFromObject(ICmObject obj)
		{
			foreach (var slice in Slices)
			{
				if (slice.MyCmObject == obj)
				{
					m_fSetCurrentSliceNew = true;
				}
				if (!m_fSetCurrentSliceNew || slice.IsHeaderNode)
				{
					continue;
				}
				m_fSetCurrentSliceNew = false;
				m_currentSliceNew = slice;
				break;
			}
		}

		/// <summary>
		/// Fixes the record list to cope with operations in detail pane
		/// that radically changes the current record.
		/// </summary>
		internal void FixRecordList()
		{
			// first update the current record to clear out invalid data associated
			// with change in the detail pane (e.g. changing morph type from stem to suffix).
			m_rlu?.RefreshCurrentRecord();
			// now fix the rest of the list, like adjusting for homograph numbers.
			m_rch?.Fixup(true);  // for adjusting homograph numbers.
		}

		private void SetCurrentSlicePropertyNames()
		{
			if (!string.IsNullOrEmpty(m_sPartNameProperty) && !string.IsNullOrEmpty(m_sObjGuidProperty))
			{
				return;
			}
			var toolChoice = PropertyTable.GetValue<string>(AreaServices.ToolChoice);
			var areaChoice = PropertyTable.GetValue<string>(AreaServices.AreaChoice);
			m_sPartNameProperty = $"{areaChoice}${toolChoice}$CurrentSlicePartName";
			m_sObjGuidProperty = $"{areaChoice}${toolChoice}$CurrentSliceObjectGuid";
		}

		/// <summary>
		/// Suspend the layout of this window and its immediate children.
		/// This version also maintains a count, and does not resume until the number of
		/// resume calls balances the number of suspend calls.
		/// </summary>
		internal void DeepSuspendLayout()
		{
			Debug.Assert(m_cDeepSuspendLayoutCount >= 0);
			if (m_cDeepSuspendLayoutCount == 0)
			{
				SuspendLayout();
			}
			m_cDeepSuspendLayoutCount++;
		}
		/// <summary>
		/// Resume the layout of this window and its immediate children.
		/// This version also maintains a count, and does not resume until the number of
		/// resume calls balances the number of suspend calls.
		/// </summary>
		internal void DeepResumeLayout()
		{
			FwUtils.CheckResumeProcessing(m_cDeepSuspendLayoutCount, GetType().Name, "DeepSuspendLayout", "DeepResumeLayout");
			m_cDeepSuspendLayoutCount--;
			if (m_cDeepSuspendLayoutCount == 0)
			{
				ResumeLayout();
			}
		}

		/// <summary>
		/// initialization for when you don't actually know what you want to show yet
		/// (and aren't going to use XML)
		/// </summary>
		protected void InitializeBasic(LcmCache cache, bool fHasSplitter)
		{
			// This has to be created before we start adding slices, so they can be put into it.
			// (Otherwise we would normally do this in initializeComponent.)
			m_fHasSplitter = fHasSplitter;
			m_mdc = cache.DomainDataByFlid.MetaDataCache;
			Cache = cache;
		}

		/// <summary>
		/// This is the initialize that is normally used. Others may not be extensively tested.
		/// </summary>
		public void Initialize(LcmCache cache, bool fHasSplitter, Inventory layouts, Inventory parts)
		{
			m_layoutInventory = layouts;
			m_partInventory = parts;
			InitializeBasic(cache, fHasSplitter);
			InitializeComponent();
			InitializeAdvanced();
		}

		protected void InitializeComponentBasic()
		{
			// Set up property change notification.
			m_sda = Cache.DomainDataByFlid;
			m_sda.AddNotification(this);
			// Currently we inherit from UserControl, which doesn't have a border. If we
			// need one various things will have to change to Panel.
			//this.BorderStyle = BorderStyle.FixedSingle;
			BackColor = Color.FromKnownColor(KnownColor.Window);
		}

		/// <summary></summary>
		protected void InitializeComponent()
		{
			InitializeComponentBasic();
		}

		private void InitializeAdvanced()
		{
			using (new DataTreeLayoutSuspensionHelper(PropertyTable.GetValue<IFwMainWnd>(FwUtils.window), this))
			{
				// NB: The ArrayList created here can hold disparate objects, such as XmlNodes and ints.
				if (Root != null)
				{
					CreateSlicesFor(Root, null, null, null, 0, 0, new ArrayList(20), new ObjSeqHashMap(), null);
				}
			}
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				var idleQueue = PropertyTable?.GetValue<IFwMainWnd>(FwUtils.window)?.IdleQueue;
				if (idleQueue != null)
				{
					idleQueue.Remove(DoPostponedFocusSlice);
					idleQueue.Remove(OnReadyToSetCurrentSlice);
				}
				PropertyTable?.RemoveProperty(LanguageExplorerConstants.DataTree);
				Subscriber.Unsubscribe(LanguageExplorerConstants.ShowHiddenFields, ShowHiddenFields_Handler);

				// Do this first, before setting m_fDisposing to true.
				m_sda?.RemoveNotification(this);

				// We'd prefer to do any cleanup of the current slice BEFORE its parent gets disposed.
				// But I can't find any event that is raised before Dispose when switching areas.
				// To avoid losing changes (e.g., in InterlinearSlice/ Words Analysis view), let the current
				// slice know it is no longer current, if we haven't already done so.
				if (m_currentSlice != null && !m_currentSlice.IsDisposed)
				{
					m_currentSlice.SetCurrentState(false);
				}

				m_currentSlice = null;

				m_fDisposing = true; // 'Disposing' isn't until we call base dispose.
				if (m_rch != null)
				{
					if (m_rch.HasRecordListUpdater)
					{
						m_rch.Fixup(false);     // no need to refresh record list on shutdown.
					}
					else
					{
						// It's fine to dispose it, after all, because m_rch has no other owner.
						m_rch.Dispose();
					}
				}
				if (m_tooltip != null)
				{
					m_tooltip.RemoveAll();
					m_tooltip.Dispose();
				}
			}
			m_sda = null;
			m_currentSlice = null;
			Root = null;
			Cache = null;
			m_mdc = null;
			m_rch = null;
			RootLayoutName = null;
			m_layoutInventory = null;
			m_partInventory = null;
			SliceFilter = null;
			m_monitoredProps = null;
			PersistenceProvder = null;
			m_styleSheet = null; // We may have made it, or been given it.
			m_tooltip = null;
			m_rlu = null;
			PropertyTable = null;
			Publisher = null;
			Subscriber = null;

			base.Dispose(disposing); // This will call Dispose on each Slice.

			if (disposing)
			{
				// Do after base dispose, since SliceTreeNode needs _dataTreeStackContextMenuFactory.LeftEdgeContextMenuFactory to not be null.
				DataTreeSliceContextMenuParameterObject?.Dispose();
			}
			DataTreeSliceContextMenuParameterObject = null;
			_sharedEventHandlers = null;
		}

		/// <summary>
		/// width of labels. Todo: this should be variable, and controlled by splitters in slices.
		/// </summary>
		public int LabelWidth => 40;

		private void PersistPreferences()
		{
			PersistenceProvder?.SetInfoObject(LanguageExplorerConstants.SliceSplitterBaseDistance, SliceSplitPositionBase);
		}

		private void RestorePreferences()
		{
			//TODO: for some reason, this can be set to only a maximum of 177. should have a minimum, not a maximum.
			SliceSplitPositionBase = (int)PersistenceProvder.GetInfoObject(LanguageExplorerConstants.SliceSplitterBaseDistance, SliceSplitPositionBase);
		}

		/// <summary>
		/// Go through each slice until we find one that needs to update its display.
		/// Helpful for reusable slices that don't get updated through RefreshList();
		/// </summary>
		private void RefreshList(int hvo, int tag)
		{
			foreach (var slice in Slices)
			{
				slice.UpdateDisplayIfNeeded(hvo, tag);
			}
			if (RefreshListNeeded)
			{
				RefreshList(false);
			}
		}

		/// <summary>
		/// Let's us know that we should do a RefreshList() to update all our non-reusable slices.
		/// </summary>
		internal bool RefreshListNeeded { get; set; }

		/// <summary>
		/// This flags whether to prevent the data tree from being "refreshed".  When going from
		/// true to false, if any refreshes were requested, one will be performed.
		/// </summary>
		public bool DoNotRefresh
		{
			get
			{
				return m_fDoNotRefresh;
			}
			set
			{
				var fOldValue = m_fDoNotRefresh;
				m_fDoNotRefresh = value;
				if (m_fDoNotRefresh || !fOldValue || !RefreshListNeeded)
				{
					return;
				}
				RefreshList(m_fPostponedClearAllSlices);
				m_fPostponedClearAllSlices = false;
			}
		}

		/// <summary>
		/// Refresh your contents. We try to re-use as many slices as possible,
		/// both to improve performance,
		/// and so as to preserve expansion state as much as possible.
		/// </summary>
		/// <param name="differentObject">
		/// True to not recycle any slices.
		/// False to try and recycle them.
		/// </param>
		/// <remarks>
		/// If the DataTree's slices call this method, they should use 'false',
		/// or they will be disposed when this call returns to them.
		/// </remarks>
		public virtual void RefreshList(bool differentObject)
		{
			if (m_fDoNotRefresh)
			{
				RefreshListNeeded = true;
				m_fPostponedClearAllSlices |= differentObject;
				return;
			}
			var fwMainWnd = PropertyTable.GetValue<IFwMainWnd>(FwUtils.window);
			using (new WaitCursor((Form)fwMainWnd))
			{
				try
				{
					var oldCurrent = m_currentSlice;
					using (new DataTreeLayoutSuspensionHelper(fwMainWnd, this))
					{
						var scrollbarPosition = VerticalScroll.Value;
						m_currentSlicePartName = string.Empty;
						m_currentSliceObjGuid = Guid.Empty;
						m_fSetCurrentSliceNew = false;
						m_currentSliceNew = null;
						XElement xnConfig = null;
						XElement xnCaller = null;
						string sLabel = null;
						Type oldType = null;
						if (m_currentSlice != null)
						{
							if (m_currentSlice.ConfigurationNode?.Parent != null)
							{
								m_currentSlicePartName = XmlUtils.GetOptionalAttributeValue(m_currentSlice.ConfigurationNode.Parent, "id", string.Empty);
							}
							if (m_currentSlice.MyCmObject != null)
							{
								m_currentSliceObjGuid = m_currentSlice.MyCmObject.Guid;
							}
							xnConfig = m_currentSlice.ConfigurationNode;
							xnCaller = m_currentSlice.CallerNode;
							sLabel = m_currentSlice.Label;
							oldType = m_currentSlice.GetType();
						}
						// Make sure we invalidate the root object if it's been deleted.
						if (Root != null && !Root.IsValidObject)
						{
							Reset();
						}
						// Make a new root object...just in case it changed class.
						Root = Root?.Cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(Root.Hvo);
						Invalidate(true); // forces all children to invalidate also
						CreateSlices(differentObject);
						PerformLayout();
						if (Slices.Contains(oldCurrent))
						{
							CurrentSlice = oldCurrent;
							m_currentSliceNew = CurrentSlice != oldCurrent ? oldCurrent : null;
						}
						else if (oldCurrent != null)
						{
							foreach (var slice in Slices)
							{
								var guidSlice = Guid.Empty;
								if (slice.MyCmObject != null)
								{
									guidSlice = slice.MyCmObject.Guid;
								}
								if (slice.GetType() == oldType && slice.CallerNode == xnCaller && slice.ConfigurationNode == xnConfig && guidSlice == m_currentSliceObjGuid && slice.Label == sLabel)
								{
									CurrentSlice = slice;
									m_currentSliceNew = CurrentSlice != slice ? slice : null;
									break;
								}
							}
						}
						// FWNX-590
						if (MiscUtils.IsMono)
						{
							VerticalScroll.Value = scrollbarPosition;
						}
						if (m_currentSlice != null)
						{
							ScrollControlIntoView(m_currentSlice);
						}
					}
				}
				finally
				{
					RefreshListNeeded = false;  // reset our flag.

					m_currentSlicePartName = null;
					m_currentSliceObjGuid = Guid.Empty;
					m_fSetCurrentSliceNew = false;
					if (m_currentSliceNew != null)
					{
						PropertyTable.GetValue<IFwMainWnd>(FwUtils.window).IdleQueue.Add(IdleQueuePriority.High, OnReadyToSetCurrentSlice, (object)false);
						// prevent setting focus in slice until we're all setup (cf.
						m_fSuspendSettingCurrentSlice = true;
					}
				}
			}
		}

		/// <summary>
		/// Create slices appropriate for current root object and layout, reusing any existing slices,
		/// and clearing out any that remain unused. If it is for a different object, reuse is more limited.
		/// </summary>
		private void CreateSlices(bool differentObject)
		{
			var wasVisible = Visible;
			var previousSlices = new ObjSeqHashMap();
			ConstructingSlices = true;
			try
			{
				// Bizarrely, calling Hide has been known to cause OnEnter to be called in a slice; we need to suppress this,
				// hence guarding it by setting ConstructingSlices.
				Hide();
				m_currentSlice?.SetCurrentState(false); // needs to know no longer current, may want to save something.
				m_currentSlice = null;
				if (differentObject)
				{
					m_currentSliceNew = null;
				}
				var dummySlices = new List<Slice>(Slices.Count);
				foreach (var slice in Slices)
				{
					slice.Visible = false;
					// dummy slices may not have keys and shouldn't be reused.
					if (slice.Key != null)
					{
						previousSlices.Add(slice.Key, slice);
					}
					else
					{
						dummySlices.Add(slice);
					}
				}
				// Does any goner have one?
				var gonnerHasToolTip = false;
				// Get rid of the dummies we aren't going to remove.
				foreach (var slice in dummySlices)
				{
					gonnerHasToolTip |= slice.ToolTip != null;
					RemoveSlice(slice);
				}
				previousSlices.ClearUnwantedPart(differentObject);
				CreateSlicesFor(Root, null, RootLayoutName, m_layoutChoiceField, 0, 0, new ArrayList(20), previousSlices, null);
				// Clear out any slices NOT reused. RemoveSlice both
				// removes them from the DataTree's controls collection and disposes them.
				foreach (var gonner in previousSlices.Values)
				{
					gonnerHasToolTip |= gonner.ToolTip != null;
					RemoveSlice(gonner);
				}
				if (gonnerHasToolTip)
				{
					// Since the dumb MS ToolTip class can't just remove one,
					// we have to remove them all and re-add the remaining ones
					// in order to have it really turn loose of the SliceTreeNode.
					m_tooltip.RemoveAll();
					foreach (var keeper in Slices)
					{
						SetToolTip(keeper);
					}
				}
				ResetTabIndices(0);
			}
			finally
			{
				ConstructingSlices = false;
			}
			if (wasVisible)
			{
				Show();
			}
		}

		/// <summary>
		/// This method is the implementation of IRefreshableRoot, which IFwMainWnd calls on all children to implement
		/// Refresh. The DataTree needs to reconstruct the list of controls, and returns true to indicate that
		/// children need not be refreshed.
		/// </summary>
		public virtual bool RefreshDisplay()
		{
			RefreshList(true);
			return true;
		}

		/// <summary>
		/// Answer true if the two slices are displaying fields of the same object.
		/// Review: should we require more strictly, that the full path of objects in their keys are the same?
		/// </summary>
		private static bool SameSourceObject(Slice first, Slice second)
		{
			return first.MyCmObject.Hvo == second.MyCmObject.Hvo;
		}

		/// <summary>
		/// Answer true if the second slice is a 'child' of the first (common key)
		/// </summary>
		private static bool IsChildSlice(Slice first, Slice second)
		{
			if (second.Key == null || second.Key.Length <= first.Key.Length)
			{
				return false;
			}
			for (var i = 0; i < first.Key.Length; i++)
			{
				var x = first.Key[i];
				var y = second.Key[i];
				// We need this ugly chunk because two distinct wrappers for the same integer
				// do not compare as equal! And we use integers (hvos) in these key lists...
				if (x != y && !(x is int && y is int && ((int)x) == ((int)y)))
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// This actually handles Paint for the contained control that has the slice controls in it.
		/// </summary>
		void HandlePaintLinesBetweenSlices(PaintEventArgs pea)
		{
			var gr = pea.Graphics;
			UserControl uc = this;
			// Where we're drawing.
			var width = uc.Width;
			using (var thinPen = new Pen(Color.LightGray, 1))
			using (var thickPen = new Pen(Color.LightGray, 1 + HeavyweightRuleThickness))
			{
				for (var i = 0; i < Slices.Count; i++)
				{
					var slice = Slices[i];
					if (slice == null)
					{
						continue;
					}
					// shouldn't be visible
					Slice nextSlice = null;
					if (i < Slices.Count - 1)
					{
						nextSlice = Slices[i + 1];
					}
					var linePen = thinPen;
					var loc = slice.Location;
					var yPos = loc.Y + slice.Height;
					var xPos = loc.X + slice.LabelIndent();

					if (nextSlice == null)
					{
						continue;
					}
					//drop the next line unless the next slice is going to be a header, too
					// (as is the case with empty sections), or isn't indented (as for the line following
					// the empty 'Subclasses' heading in each inflection class).
					if (XmlUtils.GetOptionalBooleanAttributeValue(slice.ConfigurationNode, "header", false) && nextSlice.Weight != ObjectWeight.heavy && IsChildSlice(slice, nextSlice))
					{
						continue;
					}
					//LT-11962 Improvements to display in Info tab.
					// (remove the line directly below the Notebook Record header)
					if (XmlUtils.GetOptionalBooleanAttributeValue(slice.ConfigurationNode, "skipSpacerLine", false) && slice is SummarySlice)
					{
						continue;
					}
					// Check for attribute that the next slice should be grouped with the current slice
					// regardless of whether they represent the same object.
					var fSameObject = XmlUtils.GetOptionalBooleanAttributeValue(nextSlice.ConfigurationNode, "sameObject", false);
					xPos = Math.Min(xPos, loc.X + nextSlice.LabelIndent());
					if (nextSlice.Weight == ObjectWeight.heavy)
					{
						linePen = thickPen;
						// Enhance JohnT: if HeavyweightRuleThickness is not even, may need to
						// add one more pixel here.
						yPos += HeavyweightRuleThickness / 2;
						yPos += HeavyweightRuleAboveMargin;
						//jh added
					}
					else if (fSameObject || nextSlice.Weight == ObjectWeight.light || SameSourceObject(slice, nextSlice))
					{
						xPos = SliceSplitPositionBase + Math.Min(slice.LabelIndent(), nextSlice.LabelIndent());
					}
					gr.DrawLine(linePen, xPos, yPos, width, yPos);
				}
			}
		}

		/// <summary>
		/// Return the container control to which nested controls belonging to slices should be added.
		/// This is the main DataTreeDiagram if not using a splitter, and the extra right-
		/// hand pane if using one.
		/// </summary>
		public UserControl SliceControlContainer => this;

		/// <summary />
		public LcmCache Cache { get; protected set; }

		/// <summary>
		/// Create slices for the specified object by finding a relevant template in the spec.
		/// </summary>
		/// <param name="obj">The object to make slices for.</param>
		/// <param name="parentSlice">The parent slice.</param>
		/// <param name="layoutName">Name of the layout.</param>
		/// <param name="layoutChoiceField">The layout choice field.</param>
		/// <param name="indent">The indent.</param>
		/// /// <param name="insertPosition">The insert position.</param>
		/// <param name="path">sequence of nodes and HVOs inside which this is nested</param>
		/// <param name="reuseMap">map of key/slice combinations from a DataTree being refreshed. Exact matches may be
		/// reused, and also, the expansion state of exact matches is preserved.</param>
		/// <param name="unifyWith">If not null, this is a node to be 'unified' with the one looked up
		/// using the layout name.</param>
		/// <returns>
		/// updated insertPosition for next item after the ones inserted.
		/// </returns>
		public virtual int CreateSlicesFor(ICmObject obj, Slice parentSlice, string layoutName, string layoutChoiceField, int indent, int insertPosition, ArrayList path, ObjSeqHashMap reuseMap, XElement unifyWith)
		{
			// NB: 'path' can hold either ints or XmlNodes, so a generic can't be used for it.
			if (obj == null)
			{
				return insertPosition;
			}
			var template = GetTemplateForObjLayout(obj, layoutName, layoutChoiceField);
			path.Add(template);
			var template2 = template;
			if (unifyWith != null && unifyWith.Elements().Any())
			{
				// This assumes that the attributes don't need to be unified.
				template2 = m_layoutInventory.GetUnified(template, unifyWith);
			}
			insertPosition = ApplyLayout(obj, parentSlice, template2, indent, insertPosition, path, reuseMap);
			path.RemoveAt(path.Count - 1);
			return insertPosition;
		}

		/// <summary>
		/// Get the template that should be used to display the specified object using the specified layout.
		/// </summary>
		public XElement GetTemplateForObjLayout(ICmObject obj, string layoutName, string layoutChoiceField)
		{
			var classId = obj.ClassID;
			string choiceGuidStr = null;
			if (!string.IsNullOrEmpty(layoutChoiceField))
			{
				var flid = Cache.MetaDataCacheAccessor.GetFieldId2(obj.ClassID, layoutChoiceField, true);
				m_monitoredProps.Add(Tuple.Create(obj.Hvo, flid));
				var hvo = Cache.DomainDataByFlid.get_ObjectProp(obj.Hvo, flid);
				if (hvo != 0)
				{
					choiceGuidStr = Cache.ServiceLocator.GetObject(hvo).Guid.ToString();
				}
			}
			// Custom Lists can have different selections of writing systems. LT-11941
			if (m_mdc.GetClassName(classId) == "CmCustomItem")
			{
				var owningList = (obj as ICmPossibility).OwningList;
				if (owningList == null)
				{
					layoutName = "CmPossibilityA"; // As good a default as any
				}
				else
				{
					var wss = owningList.WsSelector;
					switch (wss)
					{
						case WritingSystemServices.kwsVerns:
							layoutName = "CmPossibilityV";
							break;
						case WritingSystemServices.kwsAnals:
							layoutName = "CmPossibilityA";
							break;
						case WritingSystemServices.kwsAnalVerns:
							layoutName = "CmPossibilityAV";
							break;
						case WritingSystemServices.kwsVernAnals:
							layoutName = "CmPossibilityVA";
							break;
					}
				}
			}
			XElement template;
			var useName = layoutName ?? "default";
			var origName = useName;
			for (; ; )
			{
				var classname = m_mdc.GetClassName(classId);
				// Inventory of layouts has keys class, type, name
				template = m_layoutInventory.GetElement("layout", new[] { classname, "detail", useName, choiceGuidStr });
				if (template != null)
				{
					break;
				}
				if (obj is IRnGenericRec)
				{
					// New custom type, so we need to get the default template and add the new type. See FWR-1049
					template = m_layoutInventory.GetElement("layout", new[] { classname, "detail", useName, null });
					if (template != null)
					{
						var newTemplate = template.Clone();
						XmlUtils.SetAttribute(newTemplate, "choiceGuid", choiceGuidStr);
						m_layoutInventory.AddNodeToInventory(newTemplate);
						m_layoutInventory.PersistOverrideElement(newTemplate);
						template = newTemplate;
						break;
					}
				}
				if (classId == 0 && useName != "default")
				{
					// Nothing found all the way to CmObject...try default view.
					useName = "default";
					classId = obj.ClassID;
				}
				if (classId == 0)
				{
					// Really surprising...default view not found on CmObject??
					// This doesn't need to be localized because it's displayed in a "yellow box"
					// error report.
					throw new ApplicationException($"No matching layout found for class {classname} detail layout {origName}");
				}
				// Otherwise try superclass.
				classId = m_mdc.GetBaseClsId(classId);
			}
			return template;
		}

		/// <summary>
		/// A rather inefficient way of finding the ID of the class that has a particular name.
		/// IFwMetaDataCache should be enhanced to provide this efficiently.
		/// </summary>
		public static int GetClassId(IFwMetaDataCache mdc, string stClassName)
		{
			return mdc.GetClassId(stClassName);
		}

		/// <summary>
		/// Look for a reusable slice that matches the current path. If found, remove from map and return;
		/// otherwise, return null.
		/// </summary>
		private static Slice GetMatchingSlice(ArrayList path, ObjSeqHashMap reuseMap)
		{
			// Review JohnT(RandyR): I don't see how this can really work.
			// The original path (the key) used to set this does not, (and cannot) change,
			// but it is very common for slices to come and go, as they are inserted/deleted,
			// or when the Show hidden control is changed.
			// Those kinds of big changes will produce the input 'path' parm,
			// which has little hope of matching that fixed original key, won't it.
			// I can see how it would work when a simple F4 refresh is being done,
			// since the count of slices should remain the same.
			var list = reuseMap[path];
			if (list.Count <= 0)
			{
				return null;
			}
			var slice = (Slice)list[0];
			reuseMap.Remove(path, slice);
			return slice;

		}

		/// <summary>
		/// Apply a layout to an object, producing the specified slices.
		/// </summary>
		/// <param name="obj">The object we want a detail view of</param>
		/// <param name="parentSlice">The parent slice.</param>
		/// <param name="template">the 'layout' element</param>
		/// <param name="indent">How deeply indented the tree is at this point.</param>
		/// <param name="insertPosition">index in slices where we should insert nodes</param>
		/// <param name="path">sequence of nodes and HVOs inside which this is nested</param>
		/// <param name="reuseMap">map of key/slice combinations from a DataTree being refreshed. Exact matches may be
		/// reused, and also, the expansion state of exact matches is preserved.</param>
		/// <returns>
		/// updated insertPosition for next item after the ones inserted.
		/// </returns>
		public int ApplyLayout(ICmObject obj, Slice parentSlice, XElement template, int indent, int insertPosition, ArrayList path, ObjSeqHashMap reuseMap)
		{
			NodeTestResult ntr;
			return ApplyLayout(obj, parentSlice, template, indent, insertPosition, path, reuseMap, false, out ntr);
		}

		/// <summary>
		/// This is the guts of ApplyLayout, but it has extra arguments to allow it to be used both to actually produce
		/// slices, and just to query whether any slices will be produced.
		/// </summary>
		protected internal virtual int ApplyLayout(ICmObject obj, Slice parentSlice, XElement template, int indent, int insertPosition, ArrayList path, ObjSeqHashMap reuseMap, bool isTestOnly, out NodeTestResult testResult)
		{
			var insPos = insertPosition;
			testResult = NodeTestResult.kntrNothing;
			var cPossible = 0;
			var isCustomFieldExists = false;
			var duplicateNodes = new List<XElement>();
			// This loop handles the multiple parts of a layout.
			foreach (var partRef in template.Elements())
			{
				var refAttr = partRef.Attribute("customFields");
				if (refAttr != null)
				{
					if (isCustomFieldExists)
					{
						duplicateNodes.Add(partRef);
					}
					isCustomFieldExists = true;
				}
				// This code looks for the a special part definition with an attribute called "customFields"
				// It doesn't matter what this attribute is set to, as long as it exists.  If this attribute is
				// found, the custom fields will not be generated.
				if (XmlUtils.GetOptionalAttributeValue(partRef, "customFields", null) != null)
				{
					if (!isTestOnly)
					{
						EnsureCustomFields(obj, template, partRef);
					}

					continue;
				}
				testResult = ProcessPartRefNode(partRef, path, reuseMap, obj, parentSlice, indent, ref insPos, isTestOnly);
				if (!isTestOnly)
				{
					continue;
				}
				switch (testResult)
				{
					case NodeTestResult.kntrNothing:
						break;
					case NodeTestResult.kntrPossible:
						// nothing definite yet, but flag at least one possible.
						++cPossible;
						break;
					default:
						// if we're just looking to see if there would be any slices, and
						// there was, then don't bother thinking about any more slices.
						return insertPosition;
				}
			}
			foreach (var duplicateElement in duplicateNodes)
			{
				duplicateElement.Remove();
				m_layoutInventory.PersistOverrideElement(template);
			}
			if (cPossible > 0)
			{
				testResult = NodeTestResult.kntrPossible;   // everything else was nothing...
			}
			return insPos;
		}

		/// <summary>
		/// Process a top-level child of a layout (other than a comment).
		/// Currently these are part nodes (with ref= indicating the part to use) and sublayout nodes.
		/// </summary>
		private NodeTestResult ProcessPartRefNode(XElement partRef, ArrayList path, ObjSeqHashMap reuseMap, ICmObject obj, Slice parentSlice, int indent, ref int insPos, bool isTestOnly)
		{
			var ntr = NodeTestResult.kntrNothing;
			switch (partRef.Name.LocalName)
			{
				case "sublayout":
					// a sublayout simply includes another layout within the current layout, the layout is
					// located by name and choice field
					var layoutName = XmlUtils.GetOptionalAttributeValue(partRef, "name");
					var layoutChoiceField = XmlUtils.GetOptionalAttributeValue(partRef, "layoutChoiceField");
					var template = GetTemplateForObjLayout(obj, layoutName, layoutChoiceField);
					path.Add(partRef);
					path.Add(template);
					insPos = ApplyLayout(obj, parentSlice, template, indent, insPos, path, reuseMap, isTestOnly, out ntr);
					path.RemoveAt(path.Count - 1);
					path.RemoveAt(path.Count - 1);
					break;
				case "part":
					// If the previously selected slice doesn't display in this refresh, we try for the next
					// visible slice instead.  So m_fSetCurrentSliceNew might still be set.  See LT-9010.
					var partName = XmlUtils.GetMandatoryAttributeValue(partRef, "ref");
					if (!m_fSetCurrentSliceNew && m_currentSlicePartName != null && obj.Guid == m_currentSliceObjGuid)
					{
						for (var clid = obj.ClassID; clid != 0; clid = m_mdc.GetBaseClsId(clid))
						{
							var sFullPartName = $"{m_mdc.GetClassName(clid)}-Detail-{partName}";
							if (m_currentSlicePartName == sFullPartName)
							{
								m_fSetCurrentSliceNew = true;
								break;
							}
						}
					}
					var visibility = "always";
					if (!ShowingAllFields)
					{
						visibility = XmlUtils.GetOptionalAttributeValue(partRef, "visibility", "always");
						if (visibility == "never")
						{
							return NodeTestResult.kntrNothing;
						}
						Debug.Assert(visibility == "always" || visibility == "ifdata");
					}
					// Use the part inventory to find the indicated part.
					var classId = obj.ClassID;
					XElement part;
					for (; ; )
					{
						var classname = m_mdc.GetClassName(classId);
						// Inventory of parts has key ID. The ID is made up of the class name, "-Detail-", partname.
						var key = classname + "-Detail-" + partName;
						part = m_partInventory.GetElement("part", new[] { key });
						if (part != null)
						{
							break;
						}
						if (classId == 0) // we've just tried CmObject.
						{
							Debug.WriteLine("Warning: No matching part found for " + classname + "-Detail-" + partName);
							// Just omit the missing part.
							return NodeTestResult.kntrNothing;
						}
						// Otherwise try superclass.
						classId = m_mdc.GetBaseClsId(classId);
					}
					var parameter = XmlUtils.GetOptionalAttributeValue(partRef, "param", null);
					// If you are wondering why we put the partref in the key, one reason is that it may be needed
					// when expanding a collapsed slice.
					path.Add(partRef);
					ntr = ProcessPartChildren(part, path, reuseMap, obj, parentSlice, indent, ref insPos, isTestOnly, parameter, visibility == "ifdata", partRef);
					path.RemoveAt(path.Count - 1);
					break;
			}
			return ntr;
		}

		internal NodeTestResult ProcessPartChildren(XElement part, ArrayList path, ObjSeqHashMap reuseMap, ICmObject obj, Slice parentSlice, int indent, ref int insPos, bool isTestOnly, string parameter, bool fVisIfData, XElement caller)
		{
			// The children of the part element must now be processed. Often there is only one.
			foreach (var node in part.Elements())
			{
				var testResult = ProcessSubpartNode(node, path, reuseMap, obj, parentSlice, indent, ref insPos, isTestOnly, parameter, fVisIfData, caller);
				// If we're just looking to see if there would be any slices, and there was,
				// then don't bother thinking about any more slices.
				if (isTestOnly && testResult != NodeTestResult.kntrNothing)
				{
					return testResult;
				}
			}
			return NodeTestResult.kntrNothing; // valid if isTestOnly, otherwise don't care.
		}

		/// <summary>
		/// Append to the part refs of template a suitable one for each custom field of
		/// the class of obj.
		/// </summary>
		private void EnsureCustomFields(ICmObject obj, XElement parent, XElement insertAfter)
		{
			var interestingClasses = new HashSet<int>();
			var clsid = obj.ClassID;
			while (clsid != 0)
			{
				interestingClasses.Add(clsid);
				clsid = obj.Cache.MetaDataCacheAccessor.GetBaseClsId(clsid);
			}
			//for each custom field, we need to construct or reuse the kind of element that would normally be found in the XDE file
			foreach (var field in FieldDescription.FieldDescriptors(Cache))
			{
				if (!field.IsCustomField || !interestingClasses.Contains(field.Class))
				{
					continue;
				}
				var exists = false;
				var target = field.Name;
				var refAttr = insertAfter.Attribute("ref");
				if (refAttr == null)
				{
					var persistableParent = FindPersistableParent(parent, parent.GetOuterXml());
					insertAfter.Add(new XAttribute("ref", "_CustomFieldPlaceholder"));
					m_layoutInventory.PersistOverrideElement(persistableParent);
				}
				// We could do this search with an XPath but they are excruciatingly slow.
				var allOfUsSiblings = insertAfter.Parent.Elements().ToList();
				var whereIsWaldo = allOfUsSiblings.IndexOf(insertAfter);
				// First: Go forward if needed ("Waldo is not the last/oldest sibling").
				for (var olderSiblingIdx = whereIsWaldo + 1; olderSiblingIdx < allOfUsSiblings.Count; olderSiblingIdx++)
				{
					if (CheckCustomFieldsSibling(allOfUsSiblings[olderSiblingIdx], target))
					{
						exists = true;
					}
				}
				// Second: Go Backward if needed (Waldo is not the first/youngest sibling).
				for (var youngerSiblingIdx = whereIsWaldo - 1; youngerSiblingIdx >= 0; youngerSiblingIdx--)
				{
					if (CheckCustomFieldsSibling(allOfUsSiblings[youngerSiblingIdx], target))
					{
						exists = true;
					}
				}
				if (exists)
				{
					continue;
				}
				insertAfter.AddAfterSelf(new XElement("part", new XAttribute("ref", "Custom"), new XAttribute("param", target)));
			}
		}

		private static XElement FindPersistableParent(XElement parent, string originalParentXml)
		{
			if (Equals("part", parent.Name.LocalName) || Equals("layout", parent.Name.LocalName))
			{
				return parent;
			}
			if (parent.Parent == null)
			{
				throw new ApplicationException($"Invalid configuration file. No parent with a ref attribute was found.{Environment.NewLine}{originalParentXml}");
			}
			return FindPersistableParent(parent.Parent, originalParentXml);
		}

		private static bool CheckCustomFieldsSibling(XElement sibling, string target)
		{
			if (!sibling.Attributes().Any())
			{
				return false;   // no attributes on this node as XmlComment  LT-3566
			}
			var paramAttr = sibling.Attribute("param");
			var refAttr = sibling.Attribute("ref");
			return paramAttr != null && refAttr != null && paramAttr.Value == target && sibling.Name == "part" && refAttr.Value == "Custom";
		}

		/// <summary>
		/// Handle one (non-comment) child node of a template (or other node) being used to
		/// create slices.  Update insertPosition to indicate how many were added (it also
		/// specifies where to add).  If fTestOnly is true, do not update insertPosition, just
		/// return true if any slices would be created.  Note that this method is recursive
		/// indirectly through ProcessPartChildren().
		/// </summary>
		private NodeTestResult ProcessSubpartNode(XElement node, ArrayList path, ObjSeqHashMap reuseMap, ICmObject obj, Slice parentSlice, int indent, ref int insertPosition, bool fTestOnly, string parameter, bool fVisIfData, XElement caller)
		{
			var editor = XmlUtils.GetOptionalAttributeValue(node, "editor");
			try
			{
				editor = editor?.ToLower();
				var flid = GetFlidFromNode(node, obj);
				if (SliceFilter != null && flid != 0 && !SliceFilter.IncludeSlice(node, obj, flid, m_monitoredProps))
				{
					return NodeTestResult.kntrNothing;
				}
				switch (node.Name.LocalName)
				{
					default: // Nothing to do for unrecognized element, such as deParams.
						break;
					case "slice":
						return AddSimpleNode(path, node, reuseMap, editor, flid, obj, parentSlice, indent, ref insertPosition, fTestOnly, fVisIfData, caller);
					case "seq":
						return AddSeqNode(path, node, reuseMap, flid, obj, parentSlice, indent + Slice.ExtraIndent(node), ref insertPosition, fTestOnly, parameter, fVisIfData, caller);
					case "obj":
						return AddAtomicNode(path, node, reuseMap, flid, obj, parentSlice, indent + Slice.ExtraIndent(node), ref insertPosition, fTestOnly, parameter, fVisIfData, caller);
					case "if":
						if (XmlVc.ConditionPasses(node, obj.Hvo, Cache))
						{
							var ntr = ProcessPartChildren(node, path, reuseMap, obj, parentSlice, indent, ref insertPosition, fTestOnly, parameter, fVisIfData, caller);
							if (fTestOnly && ntr != NodeTestResult.kntrNothing)
							{
								return ntr;
							}
						}
						break;
					case "ifnot":
						if (!XmlVc.ConditionPasses(node, obj.Hvo, Cache))
						{
							var ntr = ProcessPartChildren(node, path, reuseMap, obj, parentSlice, indent, ref insertPosition, fTestOnly, parameter, fVisIfData, caller);
							if (fTestOnly && ntr != NodeTestResult.kntrNothing)
							{
								return ntr;
							}
						}
						break;
					case "choice":
						foreach (var clause in node.Elements())
						{
							if (clause.Name == "where")
							{
								if (!XmlVc.ConditionPasses(clause, obj.Hvo, Cache))
								{
									continue;
								}
								var ntr = ProcessPartChildren(clause, path, reuseMap, obj, parentSlice, indent, ref insertPosition, fTestOnly, parameter, fVisIfData, caller);
								if (fTestOnly && ntr != NodeTestResult.kntrNothing)
								{
									return ntr;
								}
								break;
								// Allow multiple where elements to be processed, but expand only
								// the first one whose condition passes.
							}
							if (clause.Name == "otherwise")
							{
								// enhance: verify last node?
								var ntr = ProcessPartChildren(clause, path, reuseMap, obj, parentSlice, indent, ref insertPosition, fTestOnly, parameter, fVisIfData, caller);
								if (fTestOnly && ntr != NodeTestResult.kntrNothing)
								{
									return ntr;
								}
								break;
							}
							throw new Exception("elements in choice must be <where...> or <otherwise>.");
						}
						break;
					case "RecordChangeHandler":
#if JASONTODO
// TODO: RBR: This was Jason's code review comment:
// TODO: Jason: "Hmm. This smells a bit. (not that you introduced this smell, you just made me sniff around it)
// TODO: So the recordchangehandler can be owned either by the DataTree or by the RecordListUpdater.
// TODO: I wonder if we can do better?""
#endif
						// No, since it isn't owned by the data tree, even though it created it.
						//if (m_rch != null && m_rch is IDisposable)
						//	(m_rch as IDisposable).Dispose();
						if (m_rch != null && !m_rch.HasRecordListUpdater)
						{
							// The above version of the Dispose call was bad,
							// when m_rlu 'owned' the m_rch.
							// Now, we know there is no 'owning' m_rlu, so we have to do it.
							m_rch.Dispose();
							m_rch = null;
						}
						m_rch = (IRecordChangeHandler)DynamicLoader.CreateObject(node, null);
						m_rch.Disposed += m_rch_Disposed;
						Debug.Assert(m_rch != null);
						m_listName = XmlUtils.GetOptionalAttributeValue(node, "listName");
						m_rlu = null;
						ResetRecordListUpdater();
						// m_rlu may still be null, but that appears to be just fine.
						m_rch.Setup(obj, m_rlu, Cache);
						return NodeTestResult.kntrNothing;
				}
			}
			catch (Exception error)
			{
				// This doesn't need to be localized because it's displayed in a "yellow box"
				// error report.
				var bldr = new StringBuilder("FieldWorks ran into a problem trying to display this object");
				bldr.AppendLine(" in DataTree::ApplyLayout: " + error.Message);
				bldr.Append("The object id was " + obj.Hvo + ".");
				if (editor != null)
				{
					bldr.AppendLine(" The editor was '" + editor + "'.");
				}
				bldr.Append(" The text of the current node was " + node.GetOuterXml());
				//now send it on
				throw new ApplicationException(bldr.ToString(), error);
			}
			// other types of child nodes, for example, parameters for jtview, don't even have
			// the potential for expansion.
			return NodeTestResult.kntrNothing;
		}

		private void m_rch_Disposed(object sender, EventArgs e)
		{
			// m_rch may not be the same RCH that was disposed, but if it was, unregister the event and clear out the data member.
			if (!ReferenceEquals(sender, m_rch))
			{
				return;
			}
			m_rch.Disposed -= m_rch_Disposed;
			m_rch = null;
		}

		private static int GetFlidFromNode(XElement node, ICmObject obj)
		{
			var attrName = XmlUtils.GetOptionalAttributeValue(node, "field");
			if ((node.Name == "if" || node.Name == "ifnot") && (XmlUtils.GetOptionalAttributeValue(node, "target", "this").ToLower() != "this" || attrName != null && attrName.IndexOf('/') != -1))
			{
				// Can't get the field value for a target other than "this", or a field that does
				// not belong directly to "this".
				return 0;
			}
			var flid = 0;
			if (attrName != null)
			{
				if (!obj.Cache.GetManagedMetaDataCache().TryGetFieldId(obj.ClassID, attrName, out flid))
				{
					throw new ApplicationException($"DataTree could not find the flid for attribute '{attrName}' of class '{obj.ClassID}'.");
				}
			}
			return flid;
		}

		private NodeTestResult AddAtomicNode(ArrayList path, XElement node, ObjSeqHashMap reuseMap, int flid, ICmObject obj, Slice parentSlice, int indent, ref int insertPosition,
			bool fTestOnly, string layoutName, bool fVisIfData, XElement caller)
		{
			// Facilitate insertion of an expandable tree node representing an owned or ref'd object.
			if (flid == 0)
			{
				throw new ApplicationException($"field attribute required for atomic properties {node.GetOuterXml()}");
			}
			var innerObj = Cache.GetAtomicPropObject(Cache.DomainDataByFlid.get_ObjectProp(obj.Hvo, flid));
			m_monitoredProps.Add(Tuple.Create(obj.Hvo, flid));
			if (fVisIfData && innerObj == null)
			{
				return NodeTestResult.kntrNothing;
			}
			if (fTestOnly)
			{
				if (innerObj != null || XmlUtils.GetOptionalAttributeValue(node, "ghost") != null)
				{
					return NodeTestResult.kntrSomething;
				}
				return NodeTestResult.kntrPossible;
			}
			path.Add(node);
			if (innerObj != null)
			{
				var layoutOverride = XmlUtils.GetOptionalAttributeValue(node, "layout", layoutName);
				var layoutChoiceField = XmlUtils.GetOptionalAttributeValue(node, "layoutChoiceField");
				path.Add(innerObj.Hvo);
				insertPosition = CreateSlicesFor(innerObj, parentSlice, layoutOverride, layoutChoiceField, indent, insertPosition, path, reuseMap, caller);
				path.RemoveAt(path.Count - 1);
			}
			else
			{
				// No inner object...do we want a ghost slice?
				if (XmlUtils.GetOptionalAttributeValue(node, "ghost") != null)
				{
					MakeGhostSlice(path, node, reuseMap, obj, parentSlice, flid, caller, indent, ref insertPosition);
				}
			}
			path.RemoveAt(path.Count - 1);
			return NodeTestResult.kntrNothing;
		}

		internal void MakeGhostSlice(ArrayList path, XElement node, ObjSeqHashMap reuseMap, ICmObject obj, Slice parentSlice, int flidEmptyProp, XElement caller, int indent, ref int insertPosition)
		{
			if (parentSlice != null)
			{
				Debug.Assert(!parentSlice.IsDisposed, "AddSimpleNode parameter 'parentSlice' is Disposed!");
			}
			var slice = GetMatchingSlice(path, reuseMap);
			if (slice == null)
			{
				slice = new GhostStringSlice(obj, flidEmptyProp, node, Cache);
				slice.InitializeFlexComponent(new FlexComponentParameters(PropertyTable, Publisher, Subscriber));
				// Set the label and abbreviation (in that order...abbr defaults to label if not given.
				// Note that we don't have a "caller" here, so we pass 'node' as both arguments...
				// means it gets searched twice if not found, but that's fairly harmless.
				slice.Label = GetLabel(node, node, obj, "ghostLabel");
				slice.Abbreviation = GetLabelAbbr(node, node, obj, slice.Label, "ghostAbbr");
				// Install new item at appropriate position and level.
				slice.Indent = indent;
				slice.MyCmObject = obj;
				slice.Cache = Cache;
				// We need a copy since we continue to modify path, so make it as compact as possible.
				slice.Key = path.ToArray();
				slice.ConfigurationNode = node;
				slice.CallerNode = caller;
				SetNodeWeight(node, slice);
				slice.FinishInit();
				InsertSliceAndRegisterWithContextHelp(insertPosition, slice);
			}
			else
			{
				EnsureValidIndexForReusedSlice(slice, insertPosition);
			}
			slice.ParentSlice = parentSlice;
			insertPosition++;
		}

		/// <summary>
		/// This provides a list of flids that lead to an object that is either being
		/// created or deleted.  This is needed to ensure that slices leading up to that
		/// object are actually created, not created as dummies which can't preserve the
		/// focus and selection properly.
		/// </summary>
		private readonly List<int> m_currentObjectFlids = new List<int>();

		/// <summary>
		/// Build a list of flids needed to expand to the slice displaying hvoOwner.
		/// </summary>
		/// <remarks>Owning Flids may not be enough.  We may need reference flids as well.
		/// This is tricky, since this is called where the slice itself isn't known.</remarks>
		internal void SetCurrentObjectFlids(int hvoOwner, int flidOwning)
		{
			m_currentObjectFlids.Clear();
			if (flidOwning != 0)
			{
				m_currentObjectFlids.Add(flidOwning);
			}
			for (var hvo = hvoOwner; hvo != 0; hvo = Cache.ServiceLocator.GetObject(hvo).Owner == null ? 0 : Cache.ServiceLocator.GetObject(hvo).Owner.Hvo)
			{
				var flid = Cache.ServiceLocator.GetObject(hvo).OwningFlid;
				if (flid != 0)
				{
					m_currentObjectFlids.Add(flid);
				}
			}
		}

		internal void ClearCurrentObjectFlids()
		{
			m_currentObjectFlids.Clear();
		}

		/// <summary>
		/// Set up monitoring, so that a change to this property will trigger reconstructing the current set of slices.
		/// </summary>
		public void MonitorProp(int hvo, int flid)
		{
			m_monitoredProps.Add(new Tuple<int, int>(hvo, flid));
		}

		/// <summary>
		/// This constant governs the decision of how many sequence items are needed before we create
		/// DummyObjectSlices instead of building the slices instantly (through CreateSlicesFor()).
		/// </summary>
		private const int kInstantSliceMax = 20;

		private NodeTestResult AddSeqNode(ArrayList path, XElement node, ObjSeqHashMap reuseMap, int flid, ICmObject obj, Slice parentSlice, int indent, ref int insertPosition, bool fTestOnly,
			string layoutName, bool fVisIfData, XElement caller)
		{
			if (flid == 0)
			{
				throw new ApplicationException($"field attribute required for seq properties {node.GetOuterXml()}");
			}
			var cobj = Cache.DomainDataByFlid.get_VecSize(obj.Hvo, flid);
			// monitor it even if we're testing: result may change.
			m_monitoredProps.Add(Tuple.Create(obj.Hvo, flid));
			if (fVisIfData && cobj == 0)
			{
				return NodeTestResult.kntrNothing;
			}
			if (fTestOnly)
			{
				if (cobj > 0 || XmlUtils.GetOptionalAttributeValue(node, "ghost") != null)
				{
					return NodeTestResult.kntrSomething;
				}

				return NodeTestResult.kntrPossible;
			}
			path.Add(node);
			var layoutOverride = XmlUtils.GetOptionalAttributeValue(node, "layout", layoutName);
			var layoutChoiceField = XmlUtils.GetOptionalAttributeValue(node, "layoutChoiceField");
			if (cobj == 0)
			{
				// Nothing in seq....do we want a ghost slice?
				if (XmlUtils.GetOptionalAttributeValue(node, "ghost") != null)
				{
					MakeGhostSlice(path, node, reuseMap, obj, parentSlice, flid, caller, indent, ref insertPosition);
				}
			}
			else if (cobj < kInstantSliceMax || // This may be a little on the small side
				m_currentObjectFlids.Contains(flid) || !string.IsNullOrEmpty(m_currentSlicePartName) && m_currentSliceObjGuid != Guid.Empty && m_currentSliceNew == null)
			{
				//Create slices immediately
				var contents = SetupContents(flid, obj);
				foreach (var hvo in contents)
				{
					path.Add(hvo);
					insertPosition = CreateSlicesFor(Cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo), parentSlice, layoutOverride, layoutChoiceField, indent, insertPosition, path, reuseMap, caller);
					path.RemoveAt(path.Count - 1);
				}
			}
			else
			{
				// Create unique DummyObjectSlices for each slice.  This may reduce the initial
				// perceived benefit, but this way doesn't crash now that the slices are being
				// disposed of.
				var cnt = 0;
				var contents = SetupContents(flid, obj);
				foreach (var hvo in contents)
				{
					var dos = new DummyObjectSlice(indent, node, (ArrayList)(path.Clone()), obj, flid, cnt, layoutOverride, layoutChoiceField, caller) { Cache = Cache, ParentSlice = parentSlice };
					path.Add(hvo);
					// This is really important. Since some slices are invisible, all must be,
					// or Show() will reorder them.
					dos.Visible = false;
					InsertSlice(insertPosition++, dos);
					path.RemoveAt(path.Count - 1);
					cnt++;
				}
			}
			path.RemoveAt(path.Count - 1);
			return NodeTestResult.kntrNothing;
		}

		private int[] SetupContents(int flid, ICmObject obj)
		{
			int[] contents;
			var chvoMax = Cache.DomainDataByFlid.get_VecSize(obj.Hvo, flid);
			using (var arrayPtr = MarshalEx.ArrayToNative<int>(chvoMax))
			{
				Cache.DomainDataByFlid.VecProp(obj.Hvo, flid, chvoMax, out chvoMax, arrayPtr);
				contents = MarshalEx.NativeToArray<int>(arrayPtr, chvoMax);
			}
			return contents;
		}

		private readonly HashSet<string> m_setInvalidFields = new HashSet<string>();

		/// <summary>
		/// This seems a bit clumsy, but the metadata cache now throws an exception if the class
		/// id/field name pair isn't valid for GetFieldId2().  Limiting this to only one throw
		/// per class/field pair seems a reasonable compromise.  To avoid all throws would
		/// require duplicating much of the metadata cache locally.
		/// </summary>
		internal int GetFlidIfPossible(int clid, string fieldName, IFwMetaDataCacheManaged mdc)
		{
			var key = fieldName + clid;
			if (m_setInvalidFields.Contains(key))
			{
				return 0;
			}
			try
			{
				return mdc.GetFieldId2(clid, fieldName, true);
			}
			catch
			{
				m_setInvalidFields.Add(key);
				return 0;
			}
		}

		/// <summary>
		/// This parses the label attribute in order to return a label from a specified field name.
		/// Currently only recognizes "$owner" to recognize the owning object, this could be expanded
		/// to include $obj or other references.
		/// </summary>
		internal string InterpretLabelAttribute(string label, ICmObject obj)
		{
			if (label == null || label.Length <= 7 || label.Substring(0, 7).ToLower() != "$owner.")
			{
				return label;
			}
			var subfield = label.Substring(7);
			var owner = obj.Owner;
			var mdc = Cache.DomainDataByFlid.MetaDataCache;
			var flidSubfield = GetFlidIfPossible(owner.ClassID, subfield, mdc as IFwMetaDataCacheManaged);
			if (flidSubfield == 0)
			{
				return label;
			}
			var type = (CellarPropertyType)Cache.DomainDataByFlid.MetaDataCache.GetFieldType(flidSubfield);
			switch (type)
			{
				default:
					Debug.Assert(type == CellarPropertyType.Unicode);
					break;
				case CellarPropertyType.MultiString:
					label = Cache.DomainDataByFlid.get_MultiStringAlt(owner.Hvo, flidSubfield, Cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle).Text;
					break;
				case CellarPropertyType.MultiUnicode:
					label = Cache.DomainDataByFlid.get_MultiStringAlt(owner.Hvo, flidSubfield, Cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle).Text;
					break;
				case CellarPropertyType.String:
					label = Cache.DomainDataByFlid.get_StringProp(owner.Hvo, flidSubfield).Text;
					break;
				case CellarPropertyType.Unicode:
					label = Cache.DomainDataByFlid.get_UnicodeProp(owner.Hvo, flidSubfield);
					break;
			}
			return label;
		}

		/// <summary>
		/// Tests to see if it should add the field (IfData), then adds the field.
		/// </summary>
		/// <returns>
		/// NodeTestResult, an enum showing if usable data is contained in the field
		/// </returns>
		private NodeTestResult AddSimpleNode(ArrayList path, XElement node, ObjSeqHashMap reuseMap, string editor, int flid, ICmObject obj, Slice parentSlice, int indent, ref int insPos,
			bool fTestOnly, bool fVisIfData, XElement caller)
		{
			var realSda = Cache.DomainDataByFlid;
			if (parentSlice != null)
			{
				Debug.Assert(!parentSlice.IsDisposed, "AddSimpleNode parameter 'parentSlice' is Disposed!");
			}
			var wsContainer = Cache.ServiceLocator.WritingSystems;
			if (fVisIfData) // Contains the tests to see if usable data is inside the field (for all types of fields)
			{
				var editorIsNull = string.IsNullOrWhiteSpace(editor);
				if (!editorIsNull)
				{
					switch (editor)
					{
						case "custom":
#if RANDYTODO
							// TODO: This branch was for the 'custom' editors, which no longer is in xml. So, figure out how to make it work again.
#endif
							Type typeFound;
							var mi = XmlViewsUtils.GetStaticMethod(node, "assemblyPath", "class", "ShowSliceForVisibleIfData", out typeFound);
							if (mi != null)
							{
								var parameters = new object[2];
								parameters[0] = node;
								parameters[1] = obj;
								var result = mi.Invoke(typeFound, System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic, null, parameters, null);
								if (!(bool)result)
								{
									return NodeTestResult.kntrNothing;
								}
							}
							break;
						case "autocustom":
							if (flid == 0)
							{
								flid = SliceFactory.GetCustomFieldFlid(caller, realSda.MetaDataCache, obj);
							}
							break;
					}
				}
				if (flid != 0)
				{
					var fieldType = (CellarPropertyType)(realSda.MetaDataCache.GetFieldType(flid) & (int)CellarPropertyTypeFilter.VirtualMask);
					switch (fieldType)
					{
						default: // if we don't know how to check, make it visible.
							break;
						// These cases are a bit tricky. We're duplicating some information here about how the slices
						// interpret their ws parameter. Don't see how to avoid it, though, without creating the slices even if not needed.
						case CellarPropertyType.MultiString:
						case CellarPropertyType.MultiUnicode:
							var ws = XmlUtils.GetOptionalAttributeValue(node, "ws", null);
							switch (ws)
							{
								case "vernacular":
									if (realSda.get_MultiStringAlt(obj.Hvo, flid, wsContainer.DefaultVernacularWritingSystem.Handle).Length == 0)
									{
										return NodeTestResult.kntrNothing;
									}
									break;
								case "analysis":
									if (realSda.get_MultiStringAlt(obj.Hvo, flid, wsContainer.DefaultAnalysisWritingSystem.Handle).Length == 0)
									{
										return NodeTestResult.kntrNothing;
									}
									break;
								default:
									if (editor == "jtview")
									{
										if (realSda.get_MultiStringAlt(obj.Hvo, flid, wsContainer.DefaultAnalysisWritingSystem.Handle).Length == 0)
										{
											return NodeTestResult.kntrNothing;
										}
									}
									// try one of the magic ones for multistring
									var wsMagic = WritingSystemServices.GetMagicWsIdFromName(ws);
									if (wsMagic == 0 && editor == "autocustom")
									{
										wsMagic = realSda.MetaDataCache.GetFieldWs(flid);
									}
									if (wsMagic == 0 && editor != "autocustom")
									{
										// not recognized, treat as visible
										break;
									}
									var rgws = WritingSystemServices.GetWritingSystemList(Cache, wsMagic, false).ToArray();
									var anyNonEmpty = false;
									foreach (var wsInst in rgws)
									{
										if (realSda.get_MultiStringAlt(obj.Hvo, flid, wsInst.Handle).Length != 0)
										{
											anyNonEmpty = true;
											break;
										}
									}
									if (!anyNonEmpty)
									{
										return NodeTestResult.kntrNothing;
									}
									break;
							}
							break;
						case CellarPropertyType.String:
							if (realSda.get_StringProp(obj.Hvo, flid).Length == 0)
							{
								return NodeTestResult.kntrNothing;
							}
							break;
						case CellarPropertyType.Unicode:
							var val = realSda.get_UnicodeProp(obj.Hvo, flid);
							if (string.IsNullOrEmpty(val))
							{
								return NodeTestResult.kntrNothing;
							}
							break;
						// Usually, the header nodes for sequences and atomic object props
						// have no editor. But sometimes they may have a jtview summary
						// or the like. If an object-prop flid is specified, check it,
						// in case we want to suppress the whole header.
						case CellarPropertyType.OwningAtomic:
						case CellarPropertyType.ReferenceAtomic:
							var hvoT = realSda.get_ObjectProp(obj.Hvo, flid);
							if (hvoT == 0)
							{
								return NodeTestResult.kntrNothing;
							}
							var objt = Cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoT);
							if (objt.ClassID == StTextTags.kClassId) // if clid is an sttext clid
							{
								var txt = (IStText)objt;
								// Test if the StText has only one paragraph
								var cpara = txt.ParagraphsOS.Count;
								if (cpara == 1)
								{
									// Tests if paragraph is empty
									var tss = ((IStTxtPara)txt.ParagraphsOS[0]).Contents;
									if (tss == null || tss.Length == 0)
									{
										return NodeTestResult.kntrNothing;
									}
								}
							}
							break;
						case CellarPropertyType.ReferenceCollection:
							// Currently this special case is only needed for ReferenceCollection (specifically for PublishIn).
							// We can broaden it if necessary, but why take the time to look for it elsewhere?
							var visibilityFlid = flid;
							var visField = XmlUtils.GetOptionalAttributeValue(node, "visField");
							if (visField != null)
							{
								var clsid = Cache.MetaDataCacheAccessor.GetOwnClsId(flid);
								visibilityFlid = Cache.MetaDataCacheAccessor.GetFieldId2(clsid, visField, true);
							}
							if (realSda.get_VecSize(obj.Hvo, visibilityFlid) == 0)
							{
								return NodeTestResult.kntrNothing;
							}
							break;
						case CellarPropertyType.OwningCollection:
						case CellarPropertyType.OwningSequence:
						case CellarPropertyType.ReferenceSequence:
							if (realSda.get_VecSize(obj.Hvo, flid) == 0)
							{
								return NodeTestResult.kntrNothing;
							}
							break;
					}
				}
				else if (editorIsNull)
				{
					// may be a summary node for a sequence or atomic node. Suppress it as well as the prop.
					XElement child = null;
					var cnodes = 0;
					foreach (var n in node.Elements())
					{
						cnodes++;
						if (cnodes > 1)
						{
							break;
						}
						child = n;
					}
					if (child != null && cnodes == 1) // exactly one non-comment child
					{
						var flidChild = GetFlidFromNode(child, obj);
						// If it's an obj or seq node and the property is empty, we'll show nothing.
						if (flidChild != 0)
						{
							if ((child.Name == "seq" || child.Name == "obj") && realSda.get_VecSize(obj.Hvo, flidChild) == 0)
							{
								return NodeTestResult.kntrNothing;
							}
						}
					}
				}
			}
			if (fTestOnly)
			{
				return NodeTestResult.kntrSomething; // slices always produce something.
			}
			path.Add(node);
			var slice = GetMatchingSlice(path, reuseMap);
			if (slice == null)
			{
				slice = SliceFactory.Create(Cache, editor, flid, node, obj, PersistenceProvder, new FlexComponentParameters(PropertyTable, Publisher, Subscriber), caller, reuseMap, _sharedEventHandlers);
				if (slice == null)
				{
					// One way this can happen in TestLangProj is with a part ref for a custom field that
					// has been deleted.
					return NodeTestResult.kntrNothing;
				}
				Debug.Assert(slice != null);
				// Set the label and abbreviation (in that order...abbr defaults to label if not given
				if (slice.Label == null)
				{
					slice.Label = GetLabel(caller, node, obj, "label");
				}
				slice.Abbreviation = GetLabelAbbr(caller, node, obj, slice.Label, "abbr");
				// Install new item at appropriate position and level.
				slice.Indent = indent;
				slice.MyCmObject = obj;
				slice.Cache = Cache;
				slice.PersistenceProvider = PersistenceProvder;
				// We need a copy since we continue to modify path, so make it as compact as possible.
				slice.Key = path.ToArray();
				slice.ConfigurationNode = node;
				slice.CallerNode = caller;
				slice.OverrideBackColor(XmlUtils.GetOptionalAttributeValue(node, "backColor"));
				SetNodeWeight(node, slice);
				slice.FinishInit();
				InsertSliceAndRegisterWithContextHelp(insPos, slice);
			}
			else
			{
				EnsureValidIndexForReusedSlice(slice, insPos);
			}
			slice.ParentSlice = parentSlice;
			insPos++;
			slice.GenerateChildren(node, caller, obj, indent, ref insPos, path, reuseMap, true);
			path.RemoveAt(path.Count - 1);

			return NodeTestResult.kntrNothing; // arbitrary what we return if not testing (see first line of method.)
		}

		/// <summary>
		/// Ensure that the reused slice is in the re-generated position.
		/// if not, it may have shifted position as a result of changing its sequence
		/// order in the database (e.g. via OnMoveUpObjectInSequence).
		/// </summary>
		private void EnsureValidIndexForReusedSlice(Slice slice, int insertPosition)
		{
			var reusedSliceIdx = slice.IndexInContainer;
			if (insertPosition != reusedSliceIdx)
			{
				ForceSliceIndex(slice, insertPosition);
			}
			Debug.Assert(slice.IndexInContainer == insertPosition, $"EnsureValideIndexFOrReusedSlice: slice '{slice.ConfigurationNode.GetOuterXml()}' at index({slice.IndexInContainer}) should have been inserted in index({insertPosition})");
			ResetTabIndices(insertPosition);
		}

		/// <summary>
		/// Get a label-like attribute for the slice.
		/// </summary>
		private string GetLabel(XElement caller, XElement node, ICmObject obj, string attr)
		{
			var label = StringTable.Table.LocalizeAttributeValue(XmlUtils.GetOptionalAttributeValue(caller, attr, null) ?? XmlUtils.GetOptionalAttributeValue(node, attr, null)
				?? XmlUtils.GetOptionalAttributeValue(caller, attr) ?? XmlUtils.GetOptionalAttributeValue(node, attr));
			return InterpretLabelAttribute(label, obj);
		}

		/// <summary>
		/// Find a suitable abbreviation for the given label.
		/// </summary>
		private string GetLabelAbbr(XElement caller, XElement node, ICmObject obj, string label, string attr)
		{
			// First see if we can find an explicit attribute value.
			var abbr = GetLabel(caller, node, obj, attr);
			if (abbr != null)
			{
				return abbr;
			}
			// Otherwise, see if we can map the label to an abbreviation in the StringTable
			if (label != null)
			{
				abbr = StringTable.Table.GetString(label, StringTable.LabelAbbreviations);
				if (abbr == "*" + label + "*")
				{
					abbr = null;    // couldn't find it in the StringTable, reset it to null.
				}
			}
			// NOTE: Currently, Slice.Abbreviation Property sets itself to a 4-char truncation of Slice.Label
			// internally when setting the property to null.  So, allow abbr == null, and let that code handle
			// the truncation.
			return InterpretLabelAttribute(abbr, obj);
		}

		private static void SetNodeWeight(XElement node, Slice slice)
		{
			var weightString = XmlUtils.GetOptionalAttributeValue(node, "weight", "field");
			ObjectWeight weight;
			switch (weightString)
			{
				case "heavy":
					weight = ObjectWeight.heavy;
					break;
				case "light":
					weight = ObjectWeight.light;
					break;
				case "normal":
					weight = ObjectWeight.normal;
					break;
				case "field":
					weight = ObjectWeight.field;
					break;
				default:
					throw new FwConfigurationException("Invalid 'weight' value, should be heavy, normal, light, or field");
			}
			slice.Weight = weight;
		}

		/// <summary>
		/// Calls ApplyLayout for each child of the argument node.
		/// </summary>
		public int ApplyChildren(ICmObject obj, Slice parentSlice, XElement template, int indent, int insertPosition, ArrayList path, ObjSeqHashMap reuseMap)
		{
			var insertPos = insertPosition;
			foreach (var node in template.Elements())
			{
				if (node.Name == "ChangeRecordHandler")
				{
					continue;   // Handle only at the top level (at least for now).
				}
				insertPos = ApplyLayout(obj, parentSlice, node, indent, insertPos, path, reuseMap);
			}
			return insertPos;
		}

		// Must be overridden if nulls will be inserted into items; when real item is needed,
		// this is called to create it.
		public virtual Slice MakeEditorAt(int i)
		{
			return null; // todo JohnT: return false;
		}

		// Get or create the real slice at index i.
		public Slice FieldAt(int i)
		{
			var slice = FieldOrDummyAt(i);
			// Keep trying until we get a real slice. It's possible, for example, that the first object
			// in a sequence expands into an embedded lazy sequence, which in turn needs to have its
			// first item made real.
			while (!slice.IsRealSlice)
			{
				var oldState = m_layoutState;
				// guard against OnPaint() while slice is being constructed. Especially dangerous if it is a view,
				// which might end up doing a re-entrant call to Construct() the root box. LT-11052.
				m_layoutState = LayoutStates.klsDoingLayout;
				try
				{
					if (slice.BecomeRealInPlace())
					{
						SetTabIndex(Slices.IndexOf(slice));
						return slice;
					}
					AboutToCreateField();
					slice.BecomeReal(i);
					RemoveSliceAt(i);
					if (i >= Slices.Count)
					{
						// BecomeReal produced nothing; range has decreased!
						return null;
					}
					// Make sure something changed; otherwise, we have an infinite loop here.
					Debug.Assert(slice != Slices[i]);
					slice = Slices[i];

				}
				finally
				{
					// If something changed the layout state during this, it probably knows what it's doing.
					// Otherwise go back to our original state.
					if (m_layoutState == LayoutStates.klsDoingLayout)
					{
						m_layoutState = oldState;
					}
				}
			}
			return slice;
		}

		/// <summary>
		/// This version expands nulls but not dummy slices. Dummy slices
		/// should know their indent.
		/// </summary>
		public Slice FieldOrDummyAt(int i)
		{
			var slice = Slices[i];
			// This cannot ever be null now that we dont; have the special SliceCollection class.
			if (slice == null)
			{
				AboutToCreateField();
				slice = MakeEditorAt(i);
				RawSetSlice(i, slice);
			}
			return slice;
		}

		/// <summary>
		/// Intended to be called by Datatree.FieldAt just before it creates a new slice.
		/// </summary>
		internal void AboutToCreateField()
		{
			if (m_layoutState != LayoutStates.klsChecking)
			{
				return;
			}
			SuspendLayout();
			m_layoutState = LayoutStates.klsLayoutSuspended;
		}

		public bool PrepareToGoAway()
		{
			string sCurrentPartName = null;
			var guidCurrentObj = Guid.Empty;
			if (m_currentSlice != null)
			{
				if (m_currentSlice.ConfigurationNode?.Parent != null)
				{
					sCurrentPartName = XmlUtils.GetOptionalAttributeValue(m_currentSlice.ConfigurationNode.Parent, "id", string.Empty);
				}

				if (m_currentSlice.MyCmObject != null)
				{
					guidCurrentObj = m_currentSlice.MyCmObject.Guid;
				}
			}
			SetCurrentSlicePropertyNames();
			PropertyTable.SetProperty(m_sPartNameProperty, sCurrentPartName, true, settingsGroup: SettingsGroup.LocalSettings);
			PropertyTable.SetProperty(m_sObjGuidProperty, guidCurrentObj, true, settingsGroup: SettingsGroup.LocalSettings);
			return true;
		}

		protected override void OnLayout(LayoutEventArgs levent)
		{
			if (m_layoutState == LayoutStates.klsChecking)
			{
				SuspendLayout();
				m_layoutState = LayoutStates.klsLayoutSuspended;
				return;
			}
			// doesn't seem we should ever be called with layout suspended, but it happens,
			// and we want to ignore it.
			if (m_layoutState != LayoutStates.klsNormal)
			{
				return;
			}
			var fNeedInternalLayout = true; // call HandleLayout1 at least once
			var smallestSize = new Size();
			// if we don't converge in three iterations, we probably never will. It is possible to get
			// in to an infinite loop, when scrollbars appear and disappear changing the width on every
			// iteration
			for (var i = 0; i < 3; i++)
			{
				var clientWidth = ClientRectangle.Width;
				// Somehow this sometimes changes the scroll position.
				// This might be reasonable if it changed the range, but it does it other times.
				// I can't figure out why, so just force it back if it does. Grrrr!
				var aspOld = AutoScrollPosition;
				base.OnLayout(levent);
				if (AutoScrollPosition != aspOld)
				{
					AutoScrollPosition = new Point(-aspOld.X, -aspOld.Y);
				}
				if (smallestSize.IsEmpty || ClientSize.Width < smallestSize.Width)
				{
					smallestSize = ClientSize;
				}
				// If that changed the width of our client rectangle we definitely need to
				// call HandleLayout1 again.
				fNeedInternalLayout |= (clientWidth != ClientRectangle.Width);
				if (!fNeedInternalLayout)
				{
					return;
				}
				fNeedInternalLayout = false; // don't need to do again unless client rect width changes.
				var clipRect = ClientRectangle;
				clipRect.Offset(-AutoScrollPosition.X, -AutoScrollPosition.Y);
				m_layoutState = LayoutStates.klsDoingLayout;
				int yTop;
				try
				{
					yTop = HandleLayout1(true, clipRect);
				}
				finally
				{
					if (m_layoutState == LayoutStates.klsDoingLayout)
					{
						m_layoutState = LayoutStates.klsNormal;
					}
				}
				if (yTop != AutoScrollMinSize.Height)
				{
					AutoScrollMinSize = new Size(0, yTop);
					// If we don't do this, the system thinks only the previously hidden part of the
					// data pane was affected, whereas all of it may have been if control heights
					// changed.
					// (I suppose there could be a pathological case where two slices changed heigtht
					// by opposite amounts and we need this redraw even though the height did not change,
					// but it seems very unlikely.)
					Invalidate();
				}
				// Do the BASE.layout AGAIN...this seems to be the only way to get the scroll bars to
				// appear and disappear as required by more or less slices...
			}
			// if we make it thru all three iterations, resize to the smallest layout size, which usually
			// means scrollbars will be visible. This ensures that no content will be cut off.
			if (ClientSize.Width != smallestSize.Width)
			{
				ClientSize = smallestSize;
			}
		}

		/// <summary>
		/// Used both by main layout routine and also by OnPaint to make sure all
		/// visible slices are real. For full layout, clipRect is meaningless.
		/// </summary>
		protected internal int HandleLayout1(bool fFull, Rectangle clipRect)
		{
			if (m_fDisposing)
			{
				return clipRect.Bottom; // don't want to lay out while clearing slices in dispose!
			}
			var minHeight = GetMinFieldHeight();
			var desiredWidth = ClientRectangle.Width;
			// FWNX-370: work around https://bugzilla.novell.com/show_bug.cgi?id=609596
			if (Platform.IsMono && VerticalScroll.Visible)
			{
				desiredWidth -= SystemInformation.VerticalScrollBarWidth;
			}
			var oldPos = AutoScrollPosition;
			var desiredScrollPosition = new Point(-oldPos.X, -oldPos.Y);
			var yTop = AutoScrollPosition.Y;
			for (var i = 0; i < Slices.Count; i++)
			{
				// Don't care about items below bottom of clip, if one is specified.
				if ((!fFull) && yTop >= clipRect.Bottom)
				{
					return yTop - AutoScrollPosition.Y; // not very meaningful in this case, but a result is required.
				}
				var tci = Slices[i];
				// Best guess of its height, before we ensure it's real.
				var defHeight = tci?.Height ?? minHeight;
				var fSliceIsVisible = !fFull && yTop + defHeight > clipRect.Top && yTop <= clipRect.Bottom;
				if (fSliceIsVisible)
				{
					// We cannot allow slice to be unreal; it's visible, and we're checking
					// for real slices where they're visible
					tci = FieldAt(i); // ensures it becomes real if needed.
					var dummy = tci.Handle; // also force it to get a handle
					if (tci.Control != null)
					{
						dummy = tci.Control.Handle; // and its control must too.
					}
					if (yTop < 0)
					{
						// It starts above the top of the window. We need to adjust the scroll position
						// by the difference between the expected and actual heights.
						tci.SetWidthForDataTreeLayout(desiredWidth);
						desiredScrollPosition.Y -= (defHeight - tci.Height);
					}
				}
				if (tci == null)
				{
					yTop += minHeight;
				}
				else
				{
					// Move this slice down a little if it needs a heavy rule above it
					if (tci.Weight == ObjectWeight.heavy)
					{
						yTop += HeavyweightRuleThickness + HeavyweightRuleAboveMargin;
					}

					if (tci.Top != yTop)
					{
						tci.Top = yTop;
					}
					tci.SetWidthForDataTreeLayout(desiredWidth);
					yTop += tci.Height + 1;
					if (fSliceIsVisible)
					{
						MakeSliceVisible(tci);
					}
				}
			}
			// In the course of making slices real or adjusting their width they may have changed height (more strictly, its
			// real height may be different from the previous estimated height).
			// If it was previously above the top of the window, this can produce an unwanted
			// change in the visible position of previously visible slices.
			// The scroll position may also have changed as a result of the blankety blank
			// blank undocumented behavior of the UserControl class trying to make what it
			// thinks is the interesting child control visible.
			// In case it changed, try to change it back!
			// (This might not always succeed, if the scroll range changed so as to make the old position invalid.
			if (-AutoScrollPosition.Y != desiredScrollPosition.Y)
			{
				AutoScrollPosition = desiredScrollPosition;
			}
			return yTop - AutoScrollPosition.Y;
		}

		private void MakeSliceRealAt(int i)
		{
			// We cannot allow slice to be unreal; it's visible, and we're checking
			// for real slices where they're visible
			var oldPos = AutoScrollPosition;
			var tci = Slices[i];
			var oldHeight = tci?.Height ?? GetMinFieldHeight();
			tci = FieldAt(i); // ensures it becomes real if needed.
			var desiredWidth = ClientRectangle.Width;
			if (tci.Width != desiredWidth)
			{
				tci.SetWidthForDataTreeLayout(desiredWidth); // can have side effects, don't do unless needed.
			}
			// In the course of becoming real it may have changed height (more strictly, its
			// real height may be different from the previous estimated height).
			// If it was previously above the top of the window, this can produce an unwanted
			// change in the visible position of previously visible slices.
			// The scroll position may also have changed as a result of the blankety blank
			// blank undocumented behavior of the UserControl class trying to make what it
			// thinks is the interesting child control visible.
			// desiredScrollPosition.y is typically positive, the number of pixels hidden at the top
			// of the view before we started.
			var desiredScrollPosition = new Point(-oldPos.X, -oldPos.Y);
			// topAbs is the position of the slice relative to the top of the whole view contents now.
			var topAbs = tci.Top - AutoScrollPosition.Y;
			MakeSliceVisible(tci); // also required for it to be a real tab stop.
			if (topAbs < desiredScrollPosition.Y)
			{
				// It was above the top of the window. We need to adjust the scroll position
				// by the difference between the expected and actual heights.
				desiredScrollPosition.Y -= (oldHeight - tci.Height);
			}
			if (-AutoScrollPosition.Y != desiredScrollPosition.Y)
			{
				AutoScrollPosition = desiredScrollPosition;
			}
		}

		/// <summary>
		/// Make a slice visible, either because it needs to be drawn, or because it needs to be
		/// focused.
		/// </summary>
		internal static void MakeSliceVisible(Slice tci)
		{
			// It intersects the screen so it needs to be visible.
			if (!tci.Visible)
			{
				var index = tci.IndexInContainer;
				// All previous slices must be "visible".  Otherwise, the index of the current
				// slice gets changed when it becomes visible due to what is presumably a bug
				// in the dotnet framework.
				for (var i = 0; i < index; ++i)
				{
					Control ctrl = tci.ContainingDataTree.Slices[i];
					if (ctrl != null && !ctrl.Visible)
					{
						ctrl.Visible = true;
					}
				}
				tci.Visible = true;
				Debug.Assert(tci.IndexInContainer == index,
					string.Format("MakeSliceVisible: slice '{0}' at index({2}) should not have changed to index ({1})." +
					" This can occur when making slices visible in an order different than their order in DataTree.Slices. See LT-7307.",
					tci.ConfigurationNode?.GetOuterXml() != null ? tci.ConfigurationNode.GetOuterXml() : "(DummySlice?)", tci.IndexInContainer, index));
				// This was moved out of the Control setter because it prematurely creates
				// root boxes (because it creates a window handle). The embedded control shouldn't
				// need an accessibility name before it is visible!
				if (!string.IsNullOrEmpty(tci.Label) && tci.Control?.AccessibilityObject != null)
				{
					// + "ZZZ_Slice";
					tci.Control.AccessibilityObject.Name = tci.Label;
				}
			}
			tci.ShowSubControls();
		}

		public int GetMinFieldHeight()
		{
			return 18; // Enhance Johnt: base on default font height
		}

		/// <summary>
		/// Return the next field index that is at the specified indent level, or zero if there are no
		/// fields following this one that are at the specified level in the tree (at least not before one
		/// at a higher level). This is normally used to find the beginning of the next subrecord when we have a sequence of subrecords,
		/// and possibly sub-subrecords, with some being expanded and others not.
		/// </summary>
		/// <param name="nInd">The indent level we want.</param>
		/// <param name="iStart">An index to the current field. We start looking at the next field.</param>
		/// <returns>The index of the next field or 0 if none.</returns>
		public int NextFieldAtIndent(int nInd, int iStart)
		{
			var cItem = Slices.Count;
			// Start at the next editor and work down, skipping more nested editors.
			for (var i = iStart + 1; i < cItem; ++i)
			{
				var nIndCur = FieldOrDummyAt(i).Indent;
				if (nIndCur == nInd) // We found another item at this level, so return it.
				{
					return i;
				}
				if (nIndCur < nInd) // We came out to a higher level, so return zero.
				{
					return 0;
				}
			}
			return 0; // Reached the end without finding one at the specified level.
		}

		/// <summary>
		/// Return the previous field index that is at the specified indent level, or zero if there are no
		/// fields preceding this one that are at the specified level in the tree (at least not before one
		/// at a higher level). This is normally used to find a parent record; some of the intermediate
		/// records may not be expanded.
		/// </summary>
		/// <param name="nInd">The indent level we want.</param>
		/// <param name="iStart">An index to the current field. We start looking at the previous field.</param>
		/// <returns>The index of the desired field or 0 if none.</returns>
		public int PrevFieldAtIndent(int nInd, int iStart)
		{
			// Start at the next editor and work down, skipping more nested editors.
			for (var i = iStart - 1; i >= 0; --i)
			{
				var nIndCur = FieldOrDummyAt(i).Indent;
				if (nIndCur == nInd) // We found another item at this level, so return it.
				{
					return i;
				}
				if (nIndCur < nInd) // We came out to a higher level, so return zero.
				{
					return 0;
				}
			}
			return 0; // Reached the start without finding one at the specified level.
		}

		/// <summary>
		/// Answer the height that the slice at index ind is considered to have.
		/// If it is null return the default size.
		/// </summary>
		private int HeightOfSliceOrNullAt(int iSlice)
		{
			var tc = Slices[iSlice];
			var dypFieldHeight = GetMinFieldHeight();
			if (tc != null)
			{
				dypFieldHeight = Math.Max(dypFieldHeight, tc.Height);
			}
			return dypFieldHeight;
		}

		/// <summary>
		/// Return the index of the slice which contains the given y position.
		/// </summary>
		/// <param name="yp">Measured from top of whole area scrolled over.</param>
		/// <returns>Index of requested slice (or -1 if after last slice)</returns>
		public int IndexOfSliceAtY(int yp)
		{
			var ypTopOfNextField = 0;
			for (var iSlice = 0; iSlice < Slices.Count; iSlice++)
			{
				var dypFieldHeight = HeightOfSliceOrNullAt(iSlice);
				ypTopOfNextField += dypFieldHeight;
				if (ypTopOfNextField > yp)
				{
					return iSlice;
				}
			}
			return -1;
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			if (m_layoutState != LayoutStates.klsNormal)
			{
				// re-entrant call, in the middle of doing layout! Suppress it. But, we need to paint sometime...
				// so queue a new paint event.
				Invalidate(false);
				return;
			}
			if (Root != null && !Root.IsValidObject)
			{
				// We got called in some bizarre state while the system is in the middle of deleting our object.
				// Safest to do nothing. Could consider invalidating, but that might become an infinite loop.
				return;
			}
			try
			{
				// Optimize JohnT: Could we do a binary search for the
				// slice at the top? But the chop point slices may not be real...
				m_layoutState = LayoutStates.klsChecking;
				var requiredReal = ClientRectangle; // all slices in this must be real
				HandleLayout1(false, requiredReal);
				var fNeedResume = (m_layoutState == LayoutStates.klsLayoutSuspended);
				m_layoutState = LayoutStates.klsNormal;
				if (fNeedResume)
				{
					var oldPos = AutoScrollPosition;
					ResumeLayout(); // will cause another paint (and may cause unwanted scroll of parent!)
					Invalidate(false); // Well, apparently doesn't always do so...we were sometimes not getting the lines. Make sure.
					PerformLayout();
					if (AutoScrollPosition != oldPos)
					{
						AutoScrollPosition = new Point(-oldPos.X, -oldPos.Y);
					}
				}
				else
				{
					base.OnPaint(e);
					HandlePaintLinesBetweenSlices(e);
				}
			}
			finally
			{
				m_layoutState = LayoutStates.klsNormal;
			}
		}

		public new Control ActiveControl
		{
			set
			{
				if (base.ActiveControl == value)
				{
					return;
				}
				base.ActiveControl = value;
				foreach (var slice in Slices)
				{
					if ((slice.Control == value || slice == value) && m_currentSlice != slice)
					{
						CurrentSlice = slice;
					}
				}
			}
		}

		#region automated tree navigation

		/// <summary>
		/// Moves the focus to the first visible slice in the tree
		/// </summary>
		public void GotoFirstSlice()
		{
			GotoNextSliceAfterIndex(-1);
		}

		public Slice LastSlice => Slices.Any() ? Slices.Last() : null;

		/// <summary>
		/// Moves the focus to the next visible slice in the tree
		/// </summary>
		public void GotoNextSlice()
		{
			if (m_currentSlice != null)
			{
				GotoNextSliceAfterIndex(Slices.IndexOf(m_currentSlice));
			}
		}

		internal bool GotoNextSliceAfterIndex(int index)
		{
			++index;
			while (index >= 0 && index < Slices.Count)
			{
				var current = FieldAt(index);
				MakeSliceVisible(current);
				if (current.TakeFocus(false))
				{
					if (m_currentSlice != current)
					{
						CurrentSlice = current; // We are going to it, so make it current.
					}
					return true;
				}
				++index;
			}
			return false;
		}

		/// <summary>
		/// Moves the focus to the previous visible slice in the tree
		/// </summary>
		public bool GotoPreviousSliceBeforeIndex(int index)
		{
			--index;
			while (index >= 0 && index < Slices.Count)
			{
				var current = FieldAt(index);
				MakeSliceVisible(current);
				if (current.TakeFocus(false))
				{
					if (m_currentSlice != current)
					{
						CurrentSlice = current; // We are going to it, so make it current.
					}
					return true;
				}
				--index;
			}
			return false;
		}

		#endregion automated tree navigation

		#region IxCoreColleague message handlers
#if RANDYTODO
		// TODO: DataTree only handles jump stuff for menus that start with "mnuDataTree", so does nothing for menus such as: mnuEnvReferenceChoices, mnuReferenceChoices, or mnuObjectChoices.
		/// <summary>
		/// Enable menu items for jumping to the concordance (or lexiconEdit) tool.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public bool OnDisplayJumpToTool(object commandObject, ref UIItemDisplayProperties display)
		{
			string tool;
			if (display.Group != null && display.Group.IsContextMenu &&
				!string.IsNullOrEmpty(display.Group.Id) &&
				!display.Group.Id.StartsWith("mnuDataTree"))
			{
				return false;
			}
			Guid guid = GetGuidForJumpToTool((Command)commandObject, true, out tool);
			if (guid != Guid.Empty)
			{
				display.Enabled = display.Visible = true;
				return true;
			}

			return false;
		}

		/// <summary>
		/// Handle enabled menu items for jumping to another tool, or another location in the
		/// current tool.
		/// </summary>
		public bool OnJumpToTool(object commandObject)
		{
			string tool;
			Guid guid = GetGuidForJumpToTool((Command) commandObject, false, out tool);
			if (guid != Guid.Empty)
			{
				LinkHandler.PublishFollowLinkMessage(Publisher, new FwLinkArgs(tool, guid));
				((Command)commandObject).TargetId = Guid.Empty;	// clear the target for future use.
				return true;
			}

			return false;
		}

		/// <summary>
		/// Converts a List of integers into a comma-delimited string of numbers.
		/// </summary>
		private string ConvertHvoListToString(List<int> hvoList)
		{
			return XmlUtils.MakeIntegerListValue(hvoList.ToArray());
		}

		/// <summary>
		/// Common logic shared between OnDisplayJumpToTool and OnJumpToTool.
		/// forEnableOnly is true when called from OnDisplayJumpToTool.
		/// </summary>
		internal Guid GetGuidForJumpToTool(Command cmd, bool forEnableOnly, out string tool)
		{
			tool = XmlUtils.GetMandatoryAttributeValue(cmd.Parameters[0], "tool");
			string className = XmlUtils.GetMandatoryAttributeValue(cmd.Parameters[0], "className");
			ICmObject targetObject;
			if (CurrentSlice == null)
				targetObject = Root;
			else
				targetObject = CurrentSlice.Object;
			if (targetObject != null)
			{
				var owner = targetObject.Owner;
				if (tool == AreaServices.ConcordanceMachineName)
				{
					int flidSlice = 0;
					if (CurrentSlice != null && !CurrentSlice.IsHeaderNode)
					{
						flidSlice = CurrentSlice.Flid;
						if (flidSlice == 0 || m_mdc.get_IsVirtual(flidSlice))
							return cmd.TargetId;
					}
					switch (className)
					{
						case "LexEntry":
							if (m_root != null && m_root.ClassID == LexEntryTags.kClassId)
							{
								if (cmd.Id == "CmdRootEntryJumpToConcordance")
								{
									return m_root.Guid;
								}

								if (targetObject.ClassID == LexEntryRefTags.kClassId)
									return cmd.TargetId;

								if (targetObject.ClassID == LexEntryTags.kClassId)
									return targetObject.Guid;

								var lexEntry = targetObject.OwnerOfClass<ILexEntry>();
								return lexEntry == null ? cmd.TargetId : lexEntry.Guid;
							}
							break;
						case "LexSense":
							if (targetObject.ClassID == LexSenseTags.kClassId)
							{
								if (((ILexSense)targetObject).Entry == m_root)
									return targetObject.Guid;
							}
							break;
						case "MoForm":
							if (m_cache.ClassIsOrInheritsFrom(targetObject.ClassID, MoFormTags.kClassId))
							{
								if (flidSlice == MoFormTags.kflidForm)
									return targetObject.Guid;
							}
							break;
					}
				}
				else if (tool == AreaServices.LexiconEditMachineName)
				{
					if (owner != null && owner != m_root && owner.ClassID == LexEntryTags.kClassId)
					{
						return owner.Guid;
					}
				}
				else if (tool == AreaServices.NotebookEditToolMachineName)
				{
					if (owner != null &&
						owner.ClassID == RnGenericRecTags.kClassId)
						return owner.Guid;
					if (targetObject is IText)
					{
						IRnGenericRec referringRecord;
						if ((targetObject as IText).NotebookRecordRefersToThisText(out referringRecord))
							return referringRecord.Guid;

						// Text is not already associated with a notebook record. So there's nothing yet to jump to.
						// If the user is really doing the jump we need to make it now.
						// Otherwise we just need to return something non-null to indicate the jump
						// is possible (though this is not currently used).
						if (forEnableOnly)
							return targetObject.Guid;
						// User is really making the jump. Create a notebook record, associate it, and jump.
						var newNotebookRec = CreateAndAssociateNotebookRecord();
						return newNotebookRec.Guid;
					}
					// Try TargetId by default
				}
				else if (tool == AreaServices.InterlinearEditMachineName)
				{
					if (targetObject.ClassID == TextTags.kClassId)
					{
						return targetObject.Guid;
					}
				}
			}
			return cmd.TargetId;
		}

		private IRnGenericRec CreateAndAssociateNotebookRecord()
		{
			if (!(CurrentSlice.MyCmObject is IText))
			{
				throw new ArgumentException("CurrentSlice.Object ought to be a Text object.");
			}

			// Create new Notebook record
			((IText)CurrentSlice.MyCmObject).AssociateWithNotebook(true);
			IRnGenericRec referringRecord;
			(CurrentSlice.MyCmObject as IText)NotebookRecordRefersToThisText(out referringRecord);
			return referringRecord;
		}
#endif

		/// <summary>
		/// Called by reflection when a new object is inserted into the list. A change of current
		/// object should not ALWAYS make the data tree take focus, since that can be annoying when
		/// editing in the browse view (cf LT-8211). But we do want it for a new top-level list object
		/// (LT-8564). Also useful when we refresh the list after a major change to make sure something gets focused.
		/// </summary>
		public void OnFocusFirstPossibleSlice(object arg)
		{
			PropertyTable.GetValue<IFwMainWnd>(FwUtils.window).IdleQueue.Add(IdleQueuePriority.Medium, DoPostponedFocusSlice);
		}

		private bool DoPostponedFocusSlice(object parameter)
		{
			// If the user switches tools quickly after inserting an object, the new view may
			// already be created before this gets called to set the focus in the old view.
			// Therefore we don't want to crash, we just want to do nothing.  See LT-8698.
			if (IsDisposed)
			{
				throw new InvalidOperationException("Thou shalt not call methods after I am disposed!");
			}
			if (CurrentSlice == null)
			{
				FocusFirstPossibleSlice();
			}
			return true;
		}

		/// <summary>
		/// For sure make the CurrentSlice if any visible.
		/// If possible also make the preceding summary slice visible.
		/// Then make as many as possible of the slices which are children of that summary visible.
		/// </summary>
		private void ScrollCurrentAndIfPossibleSectionIntoView()
		{
			if (CurrentSlice == null)
			{
				return; // can't do anything.
			}
			// Make sure all the slices up to one screen above and below are real and valid heights.
			// This is only called in response to a user action, so m_layoutState should be normal.
			// We set this state to make quite sure that if we somehow get an OnPaint() or OnLayout call
			// that is effectively re-entrant, we don't re-enter HandleLayout1, which can really mess things up.
			Debug.Assert(m_layoutState == LayoutStates.klsNormal);
			m_layoutState = LayoutStates.klsDoingLayout;
			try
			{
				HandleLayout1(false, new Rectangle(0, Math.Max(0, CurrentSlice.Top - ClientRectangle.Height), ClientRectangle.Width, ClientRectangle.Height * 2));
			}
			finally
			{
				m_layoutState = LayoutStates.klsNormal;
			}
			ScrollControlIntoView(CurrentSlice);
			var previousSummaryIndex = CurrentSlice.IndexInContainer;
			while (!(Slices[previousSummaryIndex] is SummarySlice))
			{
				previousSummaryIndex--;
				if (previousSummaryIndex < 0)
				{
					return;
				}
			}
			var previousSummary = Slices[previousSummaryIndex];
			if (previousSummary.Top < 0 && CurrentSlice.Bottom - previousSummary.Top < ClientRectangle.Height - 20)
			{
				ScrollControlIntoView(previousSummary);
			}
			var lastChildIndex = CurrentSlice.IndexInContainer;
			while (lastChildIndex < Slices.Count && Slice.StartsWith(Slices[lastChildIndex].Key, previousSummary.Key) && Slices[lastChildIndex].Bottom - previousSummary.Top < ClientRectangle.Height - 20)
			{
				lastChildIndex++;
			}
			lastChildIndex--;
			if (lastChildIndex > CurrentSlice.IndexInContainer)
			{
				ScrollControlIntoView(Slices[lastChildIndex]);
			}
		}

		/// <summary>
		/// Find the first slice in the list which is (still) one of your current, valid slices
		/// and which is able to take focus, and give it the focus.
		/// </summary>
		internal void SelectFirstPossibleSlice(List<Slice> closeSlices)
		{
			foreach (var slice in closeSlices)
			{
				if (!slice.IsDisposed && slice.ContainingDataTree == this && slice.TakeFocus(false))
				{
					break;
				}
			}
		}

		/// <summary>
		/// Process the message to allow setting/focusing CurrentSlice.
		/// </summary>
		public bool OnReadyToSetCurrentSlice(object parameter)
		{
			if (IsDisposed)
			{
				throw new InvalidOperationException("Thou shalt not call methods after I am disposed!");
			}
			// we should now be ready to put our focus in a slice.
			m_fSuspendSettingCurrentSlice = false;
			try
			{
				SetDefaultCurrentSlice((bool?)parameter ?? false);
			}
			finally
			{
				m_fCurrentContentControlObjectTriggered = false;
			}
			return true;
		}

		/// <summary>
		/// Respond to a broadcast message.  This is needed to fix LT-9713 and LT-9714.
		/// </summary>
		public void OnDelayedRefreshList(object sentValue)
		{
			DoNotRefresh = (bool)sentValue;
		}

		/// <summary>
		/// subclasses override for setting a default current slice.
		/// </summary>
		protected virtual void SetDefaultCurrentSlice(bool suppressFocusChange)
		{
			var sliceToSetAsCurrent = m_currentSliceNew;
			m_currentSliceNew = null;
			if (sliceToSetAsCurrent != null && sliceToSetAsCurrent.IsDisposed)
			{
				sliceToSetAsCurrent = null; // someone's creating slices faster than we can display!
			}
			// try to see if any of our current slices have focus. if so, use that one.
			if (sliceToSetAsCurrent == null)
			{
				if (ContainsFocus)
				{
					// see if we can find the parent slice for focusedControl
					var currentControl = PropertyTable.GetValue<IFwMainWnd>(FwUtils.window).FocusedControl;
					while (currentControl != null && currentControl != this)
					{
						if (currentControl is Slice)
						{
							// found the slice to
							sliceToSetAsCurrent = currentControl as Slice;
							if (sliceToSetAsCurrent.IsDisposed)
							{
								sliceToSetAsCurrent = null;     // shouldn't happen, but...
							}
							else
							{
								break;
							}
						}
						currentControl = currentControl.Parent;
					}
				}
			}
			// set current slice.
			if (sliceToSetAsCurrent != null)
			{
				CurrentSlice = sliceToSetAsCurrent;
				if (!suppressFocusChange && !m_currentSlice.Focused && m_fCurrentContentControlObjectTriggered) // probably coming from m_currentSliceNew
				{
					// For string type slices, place cursor at end of (top) line.  This works
					// more reliably than putting it at the beginning for some reason, and makes
					// more sense in some circumstances (especially in the conversion from a ghost
					// slice to a string type slice).
					if (m_currentSlice is MultiStringSlice)
					{
						var mss = (MultiStringSlice)m_currentSlice;
						mss.SelectAt(mss.WritingSystemsSelectedForDisplay.First().Handle, 99999);
					}
					else if (m_currentSlice is StringSlice)
					{
						((StringSlice)m_currentSlice).SelectAt(99999);
					}
					m_currentSlice.TakeFocus(false);
				}
			}
			// otherwise, try to select the first slice, if it won't conflict with
			// an existing cursor (cf. LT-8211), like when we're first starting up/switching tools
			// as indicated by m_fCurrentContentControlObjectTriggered.
			if (!suppressFocusChange && CurrentSlice == null && m_fCurrentContentControlObjectTriggered)
			{
				FocusFirstPossibleSlice();
			}
		}

		/// <summary>
		/// Focus the first slice that can take focus.
		/// </summary>
		protected bool FocusFirstPossibleSlice()
		{
			var cslice = Slices.Count;
			// If we have a descendant that isn't the root, try to focus one of its slices.
			// Otherwise, focusing a slice that doesn't belong to it will switch the browse view
			// to a different record. See FWR-2006.
			if (Descendant != null && Descendant != Root)
			{
				var owners = new HashSet<ICmObject>();
				for (var obj = Descendant; obj != null; obj = obj.Owner)
				{
					owners.Add(obj);
				}
				for (var islice = 0; islice < cslice; ++islice)
				{
					var slice = FieldOrDummyAt(islice);
					if (slice is DummyObjectSlice && owners.Contains(slice.MyCmObject))
					{
						// This is what we want! Expand it!
						slice = FieldAt(islice); // makes a real slice (and may create children, altering the total number).
						cslice = Slices.Count;
					}

					if (Descendant != DescendantForSlice(slice))
					{
						continue;
					}

					if (slice.TakeFocus(false))
					{
						return true;
					}
				}
			}
			// If that didn't work or we don't have a distinct descendant, just focus the first thing we can.
			for (var islice = 0; islice < cslice; ++islice)
			{
				var slice = Slices[islice];
				if (slice.TakeFocus(false))
				{
					return true;
				}
			}
			return false;
		}

		#endregion IxCoreColleague message handlers

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

		#region Implementation of IFlexComonent

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		public virtual void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			FlexComponentParameters.CheckInitializationValues(flexComponentParameters, new FlexComponentParameters(PropertyTable, Publisher, Subscriber));

			PropertyTable = flexComponentParameters.PropertyTable;
			Publisher = flexComponentParameters.Publisher;
			Subscriber = flexComponentParameters.Subscriber;

			PropertyTable.SetProperty(LanguageExplorerConstants.DataTree, this, settingsGroup: SettingsGroup.LocalSettings);
			Subscriber.Subscribe(LanguageExplorerConstants.ShowHiddenFields, ShowHiddenFields_Handler);
			if (PersistenceProvder != null)
			{
				RestorePreferences();
			}
		}

		#endregion

		private void ShowHiddenFields_Handler(object obj)
		{
			var newShowValue = (bool)obj;
			if (newShowValue == ShowingAllFields)
			{
				return;
			}
			MonoIgnoreUpdates();
			try
			{
				var closeSlices = CurrentSlice?.GetNearbySlices();
				ShowingAllFields = newShowValue;
				RefreshList(false);
				if (closeSlices != null)
				{
					SelectFirstPossibleSlice(closeSlices);
				}
				ScrollCurrentAndIfPossibleSectionIntoView();
			}
			finally
			{
				MonoResumeUpdates();
			}
		}
	}
}