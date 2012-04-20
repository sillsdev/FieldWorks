using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;

using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.Common.Framework.DetailControls
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
	public class DataTree : UserControl, IFWDisposable, IVwNotifyChange, IxCoreColleague, IRefreshableRoot
	{
		/// <summary>
		/// Occurs when the current slice changes
		/// </summary>
		public event EventHandler CurrentSliceChanged;

		#region Data members

		/// <summary>
		/// Use this to do the Add/RemoveNotifications, since it can be used in the unmanged section of Dispose.
		/// (If m_sda is COM, that is.)
		/// Doing it there will be safer, since there was a risk of it not being removed
		/// in the mananged section, as when disposing was done by the Finalizer.
		/// </summary>
		private ISilDataAccess m_sda;
		/// <summary></summary>
		protected FdoCache m_cache;
		/// <summary>use SetContextMenuHandler() to subscribe to this event (if you want to provide a Context menu for this DataTree)</summary>
		protected event SliceShowMenuRequestHandler ShowContextMenuEvent;
		//protected AutoDataTreeMenuHandler m_autoHandler;
		/// <summary></summary>
		/// <summary>the descendent object that is being displayed</summary>
		protected ICmObject m_descendant;
		/// <summary></summary>
		protected IFwMetaDataCache m_mdc; // allows us to interpret class and field names and trace superclasses.
		/// <summary></summary>
		protected ICmObject m_root;
		/// <summary></summary>
		protected Slice m_currentSlice = null;
		/// <summary>used to restore current slice during RefreshList()</summary>
		private string m_currentSlicePartName = null;
		/// <summary>used to restore current slice during RefreshList()</summary>
		private Guid m_currentSliceObjGuid = Guid.Empty;
		/// <summary>used to restore current slice during RefreshList()</summary>
		private bool m_fSetCurrentSliceNew = false;
		/// <summary>used to restore current slice during RefreshList()</summary>
		private Slice m_currentSliceNew = null;
		/// <summary>used to restore current slice during RefreshList()</summary>
		private string m_sPartNameProperty = null;
		/// <summary>used to restore current slice during RefreshList()</summary>
		private string m_sObjGuidProperty = null;
		/// <summary></summary>
		protected ImageCollection m_smallImages = null;
		/// <summary></summary>
		protected string m_rootLayoutName = "default";
		/// <summary></summary>
		protected string m_layoutChoiceField;
		/// <summary>This is the position a splitter would be if we had a single one, the actual
		/// position of the splitter in a zero-indent slice. This is persisted, and can also be
		/// controlled by the XML file; the value here is a last-resort default.
		/// </summary>
		protected int m_sliceSplitPositionBase = 150;
		/// <summary>
		/// This XML document object holds the nodes that we create on-the-fly to represent custom fields.
		/// </summary>
		protected XmlDocument m_autoCustomFieldNodesDocument;
		/// <summary></summary>
		protected XmlNode m_autoCustomFieldNodesDocRoot;
		/// <summary></summary>
		protected Inventory m_layoutInventory; // inventory of layouts for different object classes.
		/// <summary></summary>
		protected Inventory m_partInventory; // inventory of parts used in layouts.
		/// <summary></summary>
		protected internal bool m_fHasSplitter;
		/// <summary></summary>
		protected SliceFilter m_sliceFilter;

		/// <summary>Set of KeyValuePair objects (hvo, flid), properties for which we must refresh if altered.</summary>
		protected HashSet<Tuple<int, int>> m_monitoredProps = new HashSet<Tuple<int, int>>();
		// Number of times DeepSuspendLayout has been called without matching DeepResumeLayout.
		protected int m_cDeepSuspendLayoutCount;
		protected StringTable m_stringTable;
		protected IPersistenceProvider m_persistenceProvider = null;
		protected FwStyleSheet m_styleSheet;
		protected bool m_fShowAllFields = false;
		protected ToolTip m_tooltip; // used for slice tree nodes. All tooltips are cleared when we switch records!
		protected LayoutStates m_layoutState = LayoutStates.klsNormal;
		protected int m_dxpLastRightPaneWidth = -1;  // width of right pane (if any) the last time we did a layout.
		// to allow slices to handle events (e.g. InflAffixTemplateSlice)
		protected Mediator m_mediator;
		protected IRecordChangeHandler m_rch = null;
		protected IRecordListUpdater m_rlu = null;
		protected string m_listName;
		bool m_fDisposing = false;
		bool m_fRefreshListNeeded = false;
		/// <summary>
		/// this helps DataTree delay from setting focus in a slice, until we're all setup to do so.
		/// </summary>
		bool m_fSuspendSettingCurrentSlice = false;
		bool m_fCurrentContentControlObjectTriggered = false;

		/// <summary>
		/// These variables are used to prevent refreshes from occurring when they're not wanted,
		/// but then to do a refresh when it's safe.
		/// </summary>
		bool m_fDoNotRefresh = false;
		bool m_fPostponedClearAllSlices = false;
		// Set during ConstructSlices, to suppress certain behaviors not safe at this point.
		internal bool ConstructingSlices { get; private set; }

		public List<Slice> Slices { get; private set; }

		#endregion Data members

		#region constants

		/// <summary></summary>
		public const int HeavyweightRuleThickness = 2;
		/// <summary></summary>
		public const int HeavyweightRuleAboveMargin = 10;

		#endregion constants

		#region enums

		public enum TreeItemState: byte
		{
			ktisCollapsed,
			ktisExpanded,
			ktisFixed, // not able to expand or contract
			// Normally capable of expansion, this node has no current children, typically because it
			// expands to show a sequence and the sequence is empty. We treat it like 'collapsed'
			// in that, if an object is added to the sequence, we show it. But, it is drawn as an empty
			// box, and clicking has no effect.
			ktisCollapsedEmpty
		}

		/// <summary>
		/// This is used in m_layoutStates to keep track of various special situations that
		/// affect what is done during OnLayout and HandleLayout1.
		/// </summary>
		public enum LayoutStates : byte
		{
			klsNormal, // OnLayout executes normally, nothing special is happening
			klsChecking, // OnPaint is checking that all slices that intersect the client area are ready to function.
			klsLayoutSuspended, // Had to suspend layout during paint, need to resume at end and repaint.
			klsClearingAll, // In the process of clearing all slices, ignore any intermediate layout messages.
			klsDoingLayout, // We are executing HandleLayout1 (other than from OnPaint), or laying out a single slice in FieldAt().
		}
		#endregion enums

		#region TraceSwitch methods
		/// <summary>
		/// Control how much output we send to the application's listeners (e.g. visual studio output window)
		/// </summary>
		protected TraceSwitch m_traceSwitch = new TraceSwitch("DataTree", "");
		protected void TraceVerbose(string s)
		{
			if(m_traceSwitch.TraceVerbose)
				Trace.Write(s);
		}
		protected void TraceVerboseLine(string s)
		{
			if(m_traceSwitch.TraceVerbose)
				Trace.WriteLine("DataTreeThreadID="+System.Threading.Thread.CurrentThread.GetHashCode()+": "+s);
		}
		protected void TraceInfoLine(string s)
		{
			if(m_traceSwitch.TraceInfo || m_traceSwitch.TraceVerbose)
				Trace.WriteLine("DataTreeThreadID="+System.Threading.Thread.CurrentThread.GetHashCode()+": "+s);
		}
		#endregion

		#region Slice collection manipulation methods

		private ToolTip ToolTip
		{
			get
			{
				CheckDisposed();

				if (m_tooltip == null)
				{
					m_tooltip = new ToolTip {ShowAlways = true};
				}
				return m_tooltip;
			}
		}

		private void InsertSliceAndRegisterWithContextHelp(int index, Slice slice)
		{
			slice.RegisterWithContextHelper();
			InsertSlice(index, slice);
		}

		private void InsertSlice(int index, Slice slice)
		{
			InstallSlice(slice, index);
			ResetTabIndices(index);
			if (m_fSetCurrentSliceNew && !slice.IsHeaderNode)
			{
				m_fSetCurrentSliceNew = false;
				if (m_currentSliceNew == null || m_currentSliceNew.IsDisposed)
					m_currentSliceNew = slice;
			}
		}

		private void InstallSlice(Slice slice, int index)
		{
			Debug.Assert(index >= 0 && index <= Slices.Count);

			slice.SuspendLayout();
			slice.Install(this);
			ForceSliceIndex(slice, index);
			Debug.Assert(slice.IndexInContainer == index,
				String.Format("InstallSlice: slice '{0}' at index({1}) should have been inserted in index({2}).",
				(slice.ConfigurationNode != null && slice.ConfigurationNode.OuterXml != null ? slice.ConfigurationNode.OuterXml : "(DummySlice?)"),
				slice.IndexInContainer, index));

			// Note that it is absolutely vital to do this AFTER adding the slice to the data tree.
			// Otherwise, the tooltip appears behind the form and is usually never seen.
			SetToolTip(slice);

			slice.ResumeLayout();
			// Make sure it isn't added twice.
			SplitContainer sc = slice.SplitCont;
			AdjustSliceSplitPosition(slice);
		}

		/// <summary>
		/// For some strange reason, the first Controls.SetChildIndex doesn't always put it in the specified index.
		/// The second time seems to work okay though.
		/// </summary>
		private void ForceSliceIndex(Slice slice, int index)
		{
			if (index < Slices.Count && Slices[index] != slice)
			{
				Slices.Remove(slice);
				Slices.Insert(index, slice);
			}
		}

		private void SetToolTip(Slice slice)
		{
			if (slice.ToolTip != null)
				ToolTip.SetToolTip(slice.TreeNode, slice.ToolTip);
		}

		void slice_SplitterMoved(object sender, SplitterEventArgs e)
		{
			if (m_currentSlice == null)
				return; // Too early to do much;

			// Depending on compile switch for SLICE_IS_SPLITCONTAINER,
			// the sender will be both a Slice and a SplitContainer
			// (Slice is a subclass of SplitContainer),
			// or just a SplitContainer (SplitContainer is the only child Control of a Slice).
			Slice movedSlice = sender is Slice ? (Slice) sender
				// sender is also a SplitContainer.
				: (Slice) ((SplitContainer) sender).Parent; // Have to move up one parent notch to get to teh Slice.
			if (m_currentSlice != movedSlice)
				return; // Too early to do much;

			Debug.Assert(movedSlice == m_currentSlice);

			m_sliceSplitPositionBase = movedSlice.SplitCont.SplitterDistance - movedSlice.LabelIndent();
			PersistPreferences();

			SuspendLayout();
			foreach (Slice otherSlice in Slices)
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
			movedSlice.TakeFocus();
		}

		private void AdjustSliceSplitPosition(Slice otherSlice)
		{
			SplitContainer otherSliceSC = otherSlice.SplitCont;
			// Remove and readd event handler when setting the value for the other fellow.
			otherSliceSC.SplitterMoved -= slice_SplitterMoved;
			otherSlice.SetSplitPosition();
			otherSliceSC.SplitterMoved += slice_SplitterMoved;
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);
			foreach (Slice slice in Slices)
			{
				AdjustSliceSplitPosition(slice);
			}
		}

		protected void InsertSliceRange(int insertPosition, Set<Slice> slices)
		{
			var indexableSlices = new List<Slice>(slices.ToArray());
			for (int i = indexableSlices.Count - 1; i >= 0; --i)
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
			CheckDisposed();

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

		private void RemoveSlice(Slice gonner, int index)
		{
			RemoveSlice(gonner, index, true);
		}

		/// <summary>
		/// this should ONLY be called from slice.Dispose(). It makes sure that when a slice
		/// is removed by disposing it directly it gets removed from the Slices collection.
		/// </summary>
		/// <param name="gonner"></param>
		internal void RemoveDisposedSlice(Slice gonner)
		{
			Slices.Remove(gonner);
		}

		private void RemoveSlice(Slice gonner, int index, bool fixToolTips)
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
					newCurrent = Slices[index] as Slice;
				}
				else if (Slices.Count > 0 && Slices.Count > index - 1)
				{
					// Get the one before index.
					newCurrent = Slices[index - 1] as Slice;
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
			bool gonnerHasToolTip = fixToolTips && (gonner.ToolTip != null);
			if (gonnerHasToolTip)
				m_tooltip.RemoveAll();
			gonner.Dispose();
			// Now, we need to re-add all of the surviving tooltips.
			if (gonnerHasToolTip)
			{
				foreach (Slice keeper in Slices)
					SetToolTip(keeper);
			}

			ResetTabIndices(index);
		}

		private void SetTabIndex(int index)
		{
			var slice = Slices[index];
			if (slice.IsRealSlice)
			{
				slice.TabIndex = index;
				slice.TabStop = !(slice.Control == null) && slice.Control.TabStop;
			}
		}

		/// <summary>
		/// Resets the TabIndex for all slices that are located at, or above, the <c>startingIndex</c>.
		/// </summary>
		/// <param name="startingIndex">The index to start renumbering the TabIndex.</param>
		private void ResetTabIndices(int startingIndex)
		{
			for (int i = startingIndex; i < Slices.Count; ++i)
				SetTabIndex(i);
		}

		#endregion Slice collection manipulation methods

		public DataTree()
		{
//			string objName = ToString() + GetHashCode().ToString();
//			Debug.WriteLine("Creating object:" + objName);
			Slices = new List<Slice>();
			m_autoCustomFieldNodesDocument = new XmlDocument();
			m_autoCustomFieldNodesDocRoot = m_autoCustomFieldNodesDocument.CreateElement("root");
			m_autoCustomFieldNodesDocument.AppendChild(m_autoCustomFieldNodesDocRoot);
		}

		/// <summary>
		/// Get the root layout name.
		/// </summary>
		public string RootLayoutName
		{
			// NB: The DataTree has not been written to handle swapping layouts,
			// so no 'Setter' is provided.
			get
			{
				CheckDisposed();
				return m_rootLayoutName;
			}
		}

		/// <summary>
		/// Get/Set a stylesheet suitable for use in views.
		/// Ideally, there should be just one for the whole application, so if the app has
		/// more than one datatree, do something to ensure this.
		/// Also, something external should set it if the default stylesheet (the styles
		/// stored in LangProject.Styles) is not to be used.
		/// Otherwise, the datatree will automatically load it when first requested
		/// (provided it has a cache by that time).
		/// </summary>
		public FwStyleSheet StyleSheet
		{
			get
			{
				CheckDisposed();
				if (m_styleSheet == null && m_cache != null)
				{
					m_styleSheet = new FwStyleSheet();
					m_styleSheet.Init(m_cache, m_cache.LanguageProject.Hvo,
						LangProjectTags.kflidStyles);
				}
				return m_styleSheet;
			}

			set
			{
				CheckDisposed();
				m_styleSheet = value;
			}

		}

		public void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			CheckDisposed();

#if false
			// This will be for real time updates, which is kind of expensive these days.
			if (tag == (int)FDO.Ling.LexEntry.LexEntryTags.kflidCitationForm
				|| tag == (int)FDO.Ling.MoForm.MoFormTags.kflidForm)
			{
				if (m_rch != null)
				{
					// We need to refresh the record list if homograph numbers change.
					m_rch.Fixup(true);
				}
			}
#endif
			// No, since it can only be null, if 'this' has been disposed.
			// That probably means the corresponding RemoveNotication was not done.
			// The current Dispose method has done this Remove call for quite a while now,
			// so if we still get here with it being null, something else is really broken.
			// We'll go with throwing a disposed exception for now, to see whomight still be
			// mis-behaving.
			//if (m_monitoredProps == null)
			//	return;
			if (m_monitoredProps.Contains(Tuple.Create(hvo, tag)))
			{
				RefreshList(false);
				OnFocusFirstPossibleSlice(null);
			}
			// Note, in LinguaLinks import we don't have an action handler when we hit this.
			else if (m_cache.DomainDataByFlid.GetActionHandler() != null && m_cache.DomainDataByFlid.GetActionHandler().IsUndoOrRedoInProgress)
			{
				// Redoing an Add or Undoing a Delete may not have an existing slice to work with, so just force
				// a list refresh.  See LT-6033.
				if (m_root != null && hvo == m_root.Hvo)
				{
					CellarPropertyType type = (CellarPropertyType)m_mdc.GetFieldType(tag);
					if (type == CellarPropertyType.OwningCollection ||
						type == CellarPropertyType.OwningSequence ||
						type == CellarPropertyType.ReferenceCollection ||
						type == CellarPropertyType.ReferenceSequence)
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

		/// <summary></summary>
		public Mediator Mediator
		{
			get
			{
				CheckDisposed();
				return m_mediator;
			}
		}

		/// <summary>
		/// Tells whether we are showing all fields, or just the ones requested.
		/// </summary>
		public bool ShowingAllFields
		{
			get
			{
				CheckDisposed();
				return m_fShowAllFields;
			}
		}

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
		[Browsable(false),
			DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Slice CurrentSlice
		{
			get
			{
				// Trap this before the throw to give the debugger a chance to analyze.
				Debug.Assert(m_currentSlice == null || !m_currentSlice.IsDisposed, "CurrentSlice is already disposed??");
				CheckDisposed();

				return m_currentSlice;
			}
			set
			{
				CheckDisposed();

				if (value == null)
					throw new ArgumentException("CurrentSlice on DataTree cannot be set to null. Set the underlying data member to null, if you really want it to be null.");
				Debug.Assert(!value.IsDisposed, "Setting CurrentSlice to a disposed slice -- not a good idea!");

				// don't set the current slice until we're all setup to do so (LT-7307)
				if (m_currentSlice == value || m_fSuspendSettingCurrentSlice)
					return;

				// Tell the old geezer it isn't current anymore.
				if (m_currentSlice != null)
				{
					m_currentSlice.Validate();
					if (m_currentSlice.Control is ContainerControl)
						((ContainerControl)m_currentSlice.Control).Validate();
					m_currentSlice.SetCurrentState(false);
				}

				m_currentSlice = value;

				// Tell new guy it is current.
				m_currentSlice.SetCurrentState(true);

				int index = m_currentSlice.IndexInContainer;
				ScrollControlIntoView(m_currentSlice);

				// Ensure that we can tab and shift-tab. This requires that at least one
				// following and one prior slice be a tab stop, if possible.
				for (int i = index + 1; i < Slices.Count; i++)
				{
					MakeSliceRealAt(i);
					if (Slices[i].TabStop)
						break;
				}
				for (int i = index - 1; i >= 0; i--)
				{
					MakeSliceRealAt(i);
					if (Slices[i].TabStop)
						break;
				}
				Invalidate();	// .Refresh();

				// update the current descendant
				m_descendant = DescendantForSlice(m_currentSlice);

				if (CurrentSliceChanged != null)
					CurrentSliceChanged(this, new EventArgs());
			}
		}

		/// <summary>
		/// Get the object that is considered the descendant for the given slice, that is,
		/// the object of a header node which is one of the slice's parents (or the slice itself),
		/// if possible, otherwise, the root object. May return null if the nearest Parent is disposed.
		/// </summary>
		/// <returns></returns>
		private ICmObject DescendantForSlice(Slice slice)
		{
			var loopSlice = slice;
			while (loopSlice != null && !loopSlice.IsDisposed)
			{
				// if there is not parent slice, we must be on a root slice
				if (loopSlice.ParentSlice == null)
					return m_root;
				// if we are on a header slice, the slice's object is the descendant
				if (loopSlice.IsHeaderNode)
					return loopSlice.Object.IsValidObject ? loopSlice.Object : null;
				loopSlice = loopSlice.ParentSlice;
			}
			// The following (along with the Disposed check above) prevents
			// LT-11455 Crash if we delete the last compound rule.
			// And LT-11463 Crash if Lexeme Form is filtered to Blanks.
			return m_root.IsValidObject ? m_root : null;
		}

		/// <summary>
		/// Determines whether the containing tree has a SubPossibilities slice.
		/// </summary>
		public bool HasSubPossibilitiesSlice
		{
			get
			{
				CheckDisposed();

				// Start at the end of the list, since we usually put SubPossibilities there.
				int i = Slices.Count - 1;
				for (; i >= 0; --i)
				{
					var current = (Slice) Slices[i];
					// Not sure these two cases are general enough to find a SubPossibilities slice.
					// Ideally we want to find the <seq field='SubPossibilities'> node, but in the case of
					// a header node, it's not that easy, and I (EricP) am not sure how to actually get from the
					// <part ref='SubPossibilities'> down to the seq in that case. Works for now!
					if (current.IsSequenceNode)
					{
						// see if slices is a SubPossibilities
						XmlNode node = current.ConfigurationNode.SelectSingleNode("seq");
						string field = XmlUtils.GetOptionalAttributeValue(node, "field");
						if (field == "SubPossibilities")
							break;
					}
					else if (current.IsHeaderNode)
					{
						XmlNode node = current.CallerNode.SelectSingleNode("*/part[starts-with(@ref,'SubPossibilities')]");
						if (node != null)
							break;
					}
				}
				// if we found a SubPossibilities slice, the index will be in range.
				return i >= 0 && i < Slices.Count;
			}
		}

		public ICmObject Root
		{
			get
			{
				CheckDisposed();
				return m_root;
			}
		}

		public ICmObject Descendant
		{
			get
			{
				CheckDisposed();
				return m_descendant;
			}
		}

		public void Reset()
		{
			CheckDisposed();
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
			m_currentSlice = null; //no more current slice
			// A tooltip doesn't always exist: see LT-11441, LT-11442, and LT-11444.
			if (m_tooltip != null)
				m_tooltip.RemoveAll();

			m_root = null;
		}

		private void ResetRecordListUpdater()
		{
			if (m_listName != null && m_rlu == null)
			{
				// Find the first parent IRecordListOwner object (if any) that
				// owns an IRecordListUpdater.
				var rlo = m_mediator.PropertyTable.GetValue("window") as IRecordListOwner;
				if (rlo != null)
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
				CheckDisposed();

				return m_sliceSplitPositionBase;
			}
			set
			{
				CheckDisposed();

				if (value == m_sliceSplitPositionBase)
					return;

				m_sliceSplitPositionBase = value;
				PersistPreferences();

				SuspendLayout();
				foreach (Slice slice in Slices)
				{
					SplitContainer sc = slice.SplitCont;
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

		public ImageCollection SmallImages
		{
			get
			{
				CheckDisposed();
				return m_smallImages;
			}
			set
			{
				CheckDisposed();
				m_smallImages = value;
			}
		}

		/// <summary>
		/// a look up table for getting the correct version of strings that the user will see.
		/// </summary>
		public SliceFilter SliceFilter
		{
			get
			{
				CheckDisposed();
				return m_sliceFilter;
			}
			set
			{
				CheckDisposed();
				m_sliceFilter = value;
			}
		}

		public StringTable StringTbl
		{
			get
			{
				CheckDisposed();
				if (m_stringTable != null)
					return m_stringTable;
				if (m_mediator != null)
					return m_mediator.StringTbl;
				return null;
			}
			set
			{
				CheckDisposed();
				m_stringTable = value;
			}
		}

		public IPersistenceProvider PersistenceProvder
		{
			set
			{
				CheckDisposed();
				m_persistenceProvider= value;
			}
			get
			{
				CheckDisposed();
				return m_persistenceProvider;
			}
		}

		private void MonoIgnoreUpdates()
		{
			#if __MonoCS__
			// static method call to get reasonable performance from mono
			// IgnoreUpdates is custom functionaily added to mono's winforms

			// Stops all winforms Size events
			Control.IgnoreUpdates();
			#endif
		}

		private void MonoResumeUpdates()
		{
			#if __MonoCS__
			// static method call to get reasonable performance from mono
			// Resumes all winforms Size events
			Control.UnignoreUpdates();
			#endif
		}

		/// <summary>
		/// Shows the specified object and makes the slices for the descendant object visible.
		/// </summary>
		/// <param name="root">The root.</param>
		/// <param name="layoutName">Name of the layout.</param>
		/// <param name="layoutChoiceField">The layout choice field.</param>
		/// <param name="descendant">The descendant.</param>
		/// <param name="suppressFocusChange">if set to <c>true</c> focus changes will be suppressed.</param>
		public virtual void ShowObject(ICmObject root, string layoutName, string layoutChoiceField, ICmObject descendant, bool suppressFocusChange)
		{
			CheckDisposed();

			if (m_root == root && layoutName == m_rootLayoutName && layoutChoiceField == m_layoutChoiceField && m_descendant == descendant)
				return;

			if (m_mediator != null) // May be null during testing or maybe some other strange state
			{
				string toolName = m_mediator.PropertyTable.GetStringProperty("currentContentControl", null);

				// Initialize our internal state with the state of the PropertyTable
				m_fShowAllFields = m_mediator.PropertyTable.GetBoolProperty("ShowHiddenFields-" + toolName, false, PropertyTable.SettingsGroup.LocalSettings);
				m_mediator.PropertyTable.SetPropertyPersistence("ShowHiddenFields-" + toolName, true, PropertyTable.SettingsGroup.LocalSettings);
				m_mediator.PropertyTable.SetDefault("ShowHiddenFields", m_fShowAllFields, false, PropertyTable.SettingsGroup.LocalSettings);
				SetCurrentSlicePropertyNames();
				m_currentSlicePartName = m_mediator.PropertyTable.GetStringProperty(m_sPartNameProperty, null, PropertyTable.SettingsGroup.LocalSettings);
				m_currentSliceObjGuid = (Guid) m_mediator.PropertyTable.GetValue(m_sObjGuidProperty, Guid.Empty, PropertyTable.SettingsGroup.LocalSettings);
				m_mediator.PropertyTable.SetProperty(m_sPartNameProperty, null, false, PropertyTable.SettingsGroup.LocalSettings);
				m_mediator.PropertyTable.SetProperty(m_sObjGuidProperty, Guid.Empty, false, PropertyTable.SettingsGroup.LocalSettings);
				m_mediator.PropertyTable.SetPropertyPersistence(m_sPartNameProperty, true, PropertyTable.SettingsGroup.LocalSettings);
				m_mediator.PropertyTable.SetPropertyPersistence(m_sObjGuidProperty, true, PropertyTable.SettingsGroup.LocalSettings);
				m_currentSliceNew = null;
				m_fSetCurrentSliceNew = false;
			}

			MonoIgnoreUpdates();

			DeepSuspendLayout();
			try
			{
				m_rootLayoutName = layoutName;
				m_layoutChoiceField = layoutChoiceField;
				Debug.Assert(m_cache != null, "You need to call Initialize() first.");

				if (m_root != root)
				{
					m_root = root;
					if (m_rch != null)
					{
						// We need to refresh the record list if homograph numbers change.
						// Do it for the old object.
						m_rch.Fixup(true);
						// Root has changed, so reset the handler.
						m_rch.Setup(m_root, m_rlu);
					}
					Invalidate(); // clears any lines left over behind slices.
					CreateSlices(true);
					if (root != descendant && (m_currentSliceNew == null || m_currentSliceNew.IsDisposed || m_currentSliceNew.Object != descendant))
						// if there is no saved current slice, or it is for the wrong object, set the current slice to be the first non-header
						// slice of the descendant object
						SetCurrentSliceNewFromObject(descendant);
				}
				else if (m_descendant != descendant)
				{
					// we are on the same root, but different descendant
					if (root != descendant)
						SetCurrentSliceNewFromObject(descendant);
				}
				else
				{
					RefreshList(false);  // This could be optimized more, too, but it isn't the common case.
				}

				m_descendant = descendant;
				AutoScrollPosition = new Point(0,0); // start new object at top (unless first focusable slice changes it).
				// We can't focus yet because the data tree slices haven't finished displaying.
				// (Remember, Windows won't let us focus something that isn't visible.)
				// (See LT-3915.)  So postpone focusing by scheduling it to execute on idle...
				// Mediator may be null during testing or maybe some other strange state
				if (m_mediator != null)
				{
					m_fCurrentContentControlObjectTriggered = true; // allow OnReadyToSetCurrentSlice to focus first possible control.
					m_mediator.IdleQueue.Add(IdleQueuePriority.High, OnReadyToSetCurrentSlice, (object) suppressFocusChange);
					// prevent setting focus in slice until we're all setup (cf.
					m_fSuspendSettingCurrentSlice = true;
				}
			}
			finally
			{
				DeepResumeLayout();

				MonoResumeUpdates();
			}
		}

		private void SetCurrentSliceNewFromObject(ICmObject obj)
		{
			foreach (Slice slice in Slices)
			{
				if (slice.Object == obj)
					m_fSetCurrentSliceNew = true;

				if (m_fSetCurrentSliceNew && !slice.IsHeaderNode)
				{
					m_fSetCurrentSliceNew = false;
					m_currentSliceNew = slice;
					break;
				}
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
			if (m_rlu != null)
				m_rlu.RefreshCurrentRecord();
			// now fix the rest of the list, like adjusting for homograph numbers.
			if (m_rch != null)
				m_rch.Fixup(true);  // for adjusting homograph numbers.
		}

		private void SetCurrentSlicePropertyNames()
		{
			if (String.IsNullOrEmpty(m_sPartNameProperty) || String.IsNullOrEmpty(m_sObjGuidProperty))
			{
				if (m_mediator != null)
				{
					string sTool = m_mediator.PropertyTable.GetStringProperty("currentContentControl", String.Empty);
					string sArea = m_mediator.PropertyTable.GetStringProperty("areaChoice", String.Empty);
					m_sPartNameProperty = String.Format("{0}${1}$CurrentSlicePartName", sArea, sTool);
					m_sObjGuidProperty = String.Format("{0}${1}$CurrentSliceObjectGuid", sArea, sTool);
				}
				else
				{
					m_sPartNameProperty = "$$CurrentSlicePartName";
					m_sObjGuidProperty = "$$CurrentSliceObjectGuid";
				}
			}
		}

		#region Sequential message processing enforcement

		public IxWindow ContainingXWindow()
		{
			CheckDisposed();

			Form form = FindForm();

			if (form is IxWindow)
				return form as IxWindow;

			return null;
		}

		/// <summary>
		/// Begin a block of code which, even though it is not itself a message handler,
		/// should not be interrupted by other messages that need to be sequential.
		/// This may be called from within a message handler.
		/// EndSequentialBlock must be called without fail (use try...finally) at the end
		/// of the block that needs protection.
		/// </summary>
		/// <returns></returns>
		public void BeginSequentialBlock()
		{
			CheckDisposed();

			IxWindow mainWindow = ContainingXWindow();
			if (mainWindow != null)
				mainWindow.SuspendIdleProcessing();
		}

		/// <summary>
		/// See BeginSequentialBlock.
		/// </summary>
		public void EndSequentialBlock()
		{
			CheckDisposed();

			IxWindow mainWindow = ContainingXWindow();
			if (mainWindow != null)
				mainWindow.ResumeIdleProcessing();
		}
		#endregion

		/// <summary>
		/// Suspend the layout of this window and its immediate children.
		/// This version also maintains a count, and does not resume until the number of
		/// resume calls balances the number of suspend calls.
		/// </summary>
		public void DeepSuspendLayout()
		{
			CheckDisposed();

			Debug.Assert(m_cDeepSuspendLayoutCount >= 0);

			if (m_cDeepSuspendLayoutCount == 0)
			{
				BeginSequentialBlock();
				SuspendLayout();
			}
			m_cDeepSuspendLayoutCount++;
		}
		/// <summary>
		/// Resume the layout of this window and its immediate children.
		/// This version also maintains a count, and does not resume until the number of
		/// resume calls balances the number of suspend calls.
		/// </summary>
		public void DeepResumeLayout()
		{
			CheckDisposed();

			Debug.Assert(m_cDeepSuspendLayoutCount > 0);

			m_cDeepSuspendLayoutCount--;
			if (m_cDeepSuspendLayoutCount == 0)
			{
				ResumeLayout();
				EndSequentialBlock();
			}
		}

		/// <summary>
		/// initialization for when you don't actually know what you want to show yet
		/// (and aren't going to use XML)
		/// </summary>
		protected void InitializeBasic(FdoCache cache, bool fHasSplitter)
		{
			//in a normal user application, this auto menu handler will not be used.
			//instead, the client of this control will call SetContextMenuHandler()
			//with a customized handler.
			// m_autoHandler = new AutoDataTreeMenuHandler(this);
			// we never use auto anymore			SetContextMenuHandler(new SliceShowMenuRequestHandler(m_autoHandler.GetSliceContextMenu));

			// This has to be created before we start adding slices, so they can be put into it.
			// (Otherwise we would normally do this in initializeComponent.)
			m_fHasSplitter = fHasSplitter;
			m_mdc = cache.DomainDataByFlid.MetaDataCache;
			m_cache = cache;
		}

		/// <summary>
		/// This is the initialize that is normally used. Others may not be extensively tested.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="fHasSplitter">if set to <c>true</c> [f has splitter].</param>
		/// <param name="layouts">The layouts.</param>
		/// <param name="parts">The parts.</param>
		public void Initialize(FdoCache cache, bool fHasSplitter, Inventory layouts, Inventory parts)
		{
			CheckDisposed();
			m_layoutInventory = layouts;
			m_partInventory = parts;
			InitializeBasic(cache, fHasSplitter);
			InitializeComponent();
		}

		protected void InitializeComponentBasic()
		{
			// Set up property change notification.
			m_sda = m_cache.DomainDataByFlid;
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
			try
			{
				DeepSuspendLayout();
				// NB: The ArrayList created here can hold disparate objects, such as XmlNodes and ints.
				if (m_root != null)
					CreateSlicesFor(m_root, null, null, null, 0, 0, new ArrayList(20), new ObjSeqHashMap(), null);
			}
			finally
			{
				DeepResumeLayout();
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged
		/// resources; <c>false</c> to release only unmanaged resources.
		/// </param>
		/// -----------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			// m_sda COM object block removed due to crash in Finializer thread LT-6124

			if (disposing)
			{
				// Do this first, before setting m_fDisposing to true.
				if (m_sda != null)
					m_sda.RemoveNotification(this);

				// We'd prefer to do any cleanup of the current slice BEFORE its parent gets disposed.
				// But I can't find any event that is raised before Dispose when switching areas.
				// To avoid losing changes (e.g., in InterlinearSlice/ Words Analysis view), let the current
				// slice know it is no longer current, if we haven't already done so.
				if (m_currentSlice != null && !m_currentSlice.IsDisposed)
					m_currentSlice.SetCurrentState(false);

				m_currentSlice = null;

				m_fDisposing = true; // 'Disposing' isn't until we call base dispose.
				if (m_rch != null)
				{
					if (m_rch.HasRecordListUpdater)
						m_rch.Fixup(false);		// no need to refresh record list on shutdown.
					else
						// It's fine to dispose it, after all, because m_rch has no other owner.
						m_rch.Dispose();
				}
				if (m_tooltip != null)
				{
					m_tooltip.RemoveAll();
					m_tooltip.Dispose();
				}
				foreach (Slice slice in Slices)
					slice.ShowContextMenu -= OnShowContextMenu;
			}
			m_sda = null;
			m_currentSlice = null;
			m_root = null;
			m_cache = null;
			m_mdc = null;
			m_autoCustomFieldNodesDocument = null;
			m_autoCustomFieldNodesDocRoot = null;
			m_rch = null;
			m_rootLayoutName = null;
			m_smallImages = null; // Client has to deal with it, since it gave it to us.
			// protected AutoDataTreeMenuHandler m_autoHandler; // No tusing this data member.
			m_layoutInventory = null;
			m_partInventory = null;
			m_sliceFilter = null;
			m_monitoredProps = null;
			m_stringTable = null;
			m_persistenceProvider = null;
			m_styleSheet = null; // We may have made it, or been given it.
			m_tooltip = null;
			m_mediator = null;
			m_rlu = null;

			base.Dispose(disposing); // This will call Dispose on each Slice.
		}

		/// <summary>
		/// Throw if the IsDisposed property is true
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException("DataTree", "This object is being used after it has been disposed: this is an Error.");
		}

		/// <summary>
		/// width of labels. Todo: this should be variable, and controlled by splitters in slices.
		/// </summary>
		public int LabelWidth
		{
			get
			{
				CheckDisposed();
				return 40;
			}
		}

		private void PersistPreferences()
		{
			if (PersistenceProvder != null)
				PersistenceProvder.SetInfoObject("SliceSplitterBaseDistance", SliceSplitPositionBase);
		}

		private void RestorePreferences()
		{
			//TODO: for some reason, this can be set to only a maximum of 177. should have a minimum, not a maximum.
			SliceSplitPositionBase = (int)PersistenceProvder.GetInfoObject("SliceSplitterBaseDistance", SliceSplitPositionBase);
		}

		/// <summary>
		/// Go through each slice until we find one that needs to update its display.
		/// Helpful for reusable slices that don't get updated through RefreshList();
		/// </summary>
		private void RefreshList(int hvo, int tag)
		{
			foreach (Slice slice in Slices)
				slice.UpdateDisplayIfNeeded(hvo, tag);

			if (RefreshListNeeded)
			{
				RefreshList(false);
			}
		}

		/// <summary>
		/// Let's us know that we should do a RefreshList() to update all our non-reusable slices.
		/// </summary>
		internal bool RefreshListNeeded
		{
			get
			{
				CheckDisposed();
				return m_fRefreshListNeeded;
			}
			set
			{
				CheckDisposed();
				m_fRefreshListNeeded = value;
			}
		}

		/// <summary>
		/// This flags whether to prevent the data tree from being "refreshed".  When going from
		/// true to false, if any refreshes were requested, one will be performed.
		/// </summary>
		public bool DoNotRefresh
		{
			get
			{
				CheckDisposed();
				return m_fDoNotRefresh;
			}
			set
			{
				CheckDisposed();
				bool fOldValue = m_fDoNotRefresh;
				m_fDoNotRefresh = value;
				if (!m_fDoNotRefresh && fOldValue && RefreshListNeeded)
				{
					RefreshList(m_fPostponedClearAllSlices);
					m_fPostponedClearAllSlices = false;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
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
		/// ------------------------------------------------------------------------------------
		public virtual void RefreshList(bool differentObject)
		{
			CheckDisposed();
			if (m_fDoNotRefresh)
			{
				RefreshListNeeded = true;
				m_fPostponedClearAllSlices |= differentObject;
				return;
			}
			Form myWindow = FindForm();
			WaitCursor wc = null;
			if (myWindow != null)
				wc = new WaitCursor(myWindow);
			try
			{
				Slice oldCurrent = m_currentSlice;
				DeepSuspendLayout();
				int scrollbarPosition = this.VerticalScroll.Value;

				m_currentSlicePartName = String.Empty;
				m_currentSliceObjGuid = Guid.Empty;
				m_fSetCurrentSliceNew = false;
				m_currentSliceNew = null;
				XmlNode xnConfig = null;
				XmlNode xnCaller = null;
				string sLabel = null;
				Type oldType = null;
				if (m_currentSlice != null)
				{
					if (m_currentSlice.ConfigurationNode != null &&
						m_currentSlice.ConfigurationNode.ParentNode != null)
					{
						m_currentSlicePartName = XmlUtils.GetAttributeValue(
							m_currentSlice.ConfigurationNode.ParentNode, "id", String.Empty);
					}
					if (m_currentSlice.Object != null)
						m_currentSliceObjGuid = m_currentSlice.Object.Guid;
					xnConfig = m_currentSlice.ConfigurationNode;
					xnCaller = m_currentSlice.CallerNode;
					sLabel = m_currentSlice.Label;
					oldType = m_currentSlice.GetType();
				}

				// Make sure we invalidate the root object if it's been deleted.
				if (m_root != null && !m_root.IsValidObject)
				{
					Reset();
				}

				// Make a new root object...just in case it changed class.
				if (m_root != null)
					m_root = m_root.Cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(m_root.Hvo);

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
					foreach (Control ctl in Slices)
					{
						var slice = ctl as Slice;
						if (slice == null)
							continue;
						Guid guidSlice = Guid.Empty;
						if (slice.Object != null)
							guidSlice = slice.Object.Guid;
						if (slice.GetType() == oldType &&
							slice.CallerNode == xnCaller &&
							slice.ConfigurationNode == xnConfig &&
							guidSlice == m_currentSliceObjGuid &&
							slice.Label == sLabel)
						{
							CurrentSlice = slice;
							m_currentSliceNew = CurrentSlice != slice ? slice : null;
							break;
						}
					}
				}

				// FWNX-590
				if (MiscUtils.IsMono)
					this.VerticalScroll.Value = scrollbarPosition;

				if (m_currentSlice != null)
				{
					ScrollControlIntoView(m_currentSlice);
				}
			}
			finally
			{
				DeepResumeLayout();
				RefreshListNeeded = false;  // reset our flag.
				if (wc != null)
				{
					wc.Dispose();
					wc = null;
				}
				m_currentSlicePartName = null;
				m_currentSliceObjGuid = Guid.Empty;
				m_fSetCurrentSliceNew = false;
				if (m_currentSliceNew != null)
				{
					m_mediator.IdleQueue.Add(IdleQueuePriority.High, OnReadyToSetCurrentSlice, (object)false);
					// prevent setting focus in slice until we're all setup (cf.
					m_fSuspendSettingCurrentSlice = true;
				}
			}
		}

		/// <summary>
		/// Create slices appropriate for current root object and layout, reusing any existing slices,
		/// and clearing out any that remain unused. If it is for a different object, reuse is more limited.
		/// </summary>
		private void CreateSlices(bool differentObject)
		{
			var watch = new Stopwatch();
			watch.Start();
			bool wasVisible = this.Visible;
			var previousSlices = new ObjSeqHashMap();
			int oldSliceCount = Slices.Count;
			ConstructingSlices = true;
			try
			{
				// Bizarrely, calling Hide has been known to cause OnEnter to be called in a slice; we need to suppress this,
				// hence guarding it by setting ConstructingSlices.
				Hide();
				if (m_currentSlice != null)
					m_currentSlice.SetCurrentState(false); // needs to know no longer current, may want to save something.
				m_currentSlice = null;
				if (differentObject)
					m_currentSliceNew = null;
				//if (differentObject)
				//	Slices.Clear();
				var dummySlices = new List<Slice>(Slices.Count);
				foreach (Slice slice in Slices)
				{
					slice.Visible = false;
					if (slice.Key != null) // dummy slices may not have keys and shouldn't be reused.
						previousSlices.Add(slice.Key, slice);
					else
						dummySlices.Add(slice);
				}
				bool gonnerHasToolTip = false; // Does any goner have one?
				// Get rid of the dummies we aren't going to remove.
				foreach (Slice slice in dummySlices)
				{
					gonnerHasToolTip |= slice.ToolTip != null;
					RemoveSlice(slice);
				}
				previousSlices.ClearUnwantedPart(differentObject);
				CreateSlicesFor(m_root, null, m_rootLayoutName, m_layoutChoiceField, 0, 0, new ArrayList(20), previousSlices, null);
				// Clear out any slices NOT reused. RemoveSlice both
				// removes them from the DataTree's controls collection and disposes them.
				foreach (Slice gonner in previousSlices.Values)
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
					foreach (Slice keeper in Slices)
						SetToolTip(keeper);
				}
				ResetTabIndices(0);
			}
			finally
			{
				ConstructingSlices = false;
			}
			if (wasVisible)
				Show();
			watch.Stop();
			// Uncomment this to investigate slice performance or issues with dissappearing slices
			//Debug.WriteLine("CreateSlices took " + watch.ElapsedMilliseconds + " ms. Originally had " + oldSliceCount + " controls; now " + Slices.Count);
			//previousSlices.Report();
		}

		protected override void OnControlAdded(ControlEventArgs e)
		{
			base.OnControlAdded(e);
		}

		protected override void OnControlRemoved(ControlEventArgs e)
		{
			base.OnControlRemoved(e);
		}

		/// <summary>
		/// This method is the implementation of IRefreshableRoot, which FwXWindow calls on all children to implement
		/// Refresh. The DataTree needs to reconstruct the list of controls, and returns true to indicate that
		/// children need not be refreshed.
		/// </summary>
		public virtual bool RefreshDisplay()
		{
			CheckDisposed();

			RefreshList(true);
			return true;
		}

		/// <summary>
		/// Answer true if the two slices are displaying fields of the same object.
		/// Review: should we require more strictly, that the full path of objects in their keys are the same?
		/// </summary>
		private static bool SameSourceObject(Slice first, Slice second)
		{
			return first.Object.Hvo == second.Object.Hvo;
		}

		/// <summary>
		/// Answer true if the second slice is a 'child' of the first (common key)
		/// </summary>
		private static bool IsChildSlice(Slice first, Slice second)
		{
			if (second.Key == null || second.Key.Length <= first.Key.Length)
				return false;
			for (int i = 0; i < first.Key.Length; i++)
			{
				object x = first.Key[i];
				object y = second.Key[i];
				// We need this ugly chunk because two distinct wrappers for the same integer
				// do not compare as equal! And we use integers (hvos) in these key lists...
				if (x != y && !(x is int && y is int && ((int)x) == ((int)y)))
					return false;
			}
			return true;
		}

		/// <summary>
		/// This actually handles Paint for the contained control that has the slice controls in it.
		/// </summary>
		/// <param name="pea">The <see cref="System.Windows.Forms.PaintEventArgs"/> instance containing the event data.</param>
		void HandlePaintLinesBetweenSlices(PaintEventArgs pea)
		{
			Graphics gr = pea.Graphics;
			UserControl uc = this;
			// Where we're drawing.
			int width = uc.Width;
			using (var thinPen = new Pen(Color.LightGray, 1))
			using (var thickPen = new Pen(Color.LightGray, 1 + HeavyweightRuleThickness))
			{
			for (int i = 0; i < Slices.Count; i++)
			{
				var slice = Slices[i] as Slice;
				if (slice == null)
						continue;
					// shouldn't be visible
				Slice nextSlice = null;
				if (i < Slices.Count - 1)
					nextSlice = Slices[i + 1] as Slice;
				Pen linePen = thinPen;
				Point loc = slice.Location;
				int yPos = loc.Y + slice.Height;
				int xPos = loc.X + slice.LabelIndent();

				if (nextSlice != null)
				{
					// Skip drawing line between two adjacent summaries.
					//					if (nextSlice is SummarySlice && slice is SummarySlice)
					//						continue;
					//drop the next line unless the next slice is going to be a header, too
					// (as is the case with empty sections), or isn't indented (as for the line following
					// the empty 'Subclasses' heading in each inflection class).
					if (XmlUtils.GetOptionalBooleanAttributeValue(slice.ConfigurationNode, "header", false)
						&& nextSlice.Weight != ObjectWeight.heavy && IsChildSlice(slice, nextSlice))
						continue;

					//LT-11962 Improvements to display in Info tab.
					// (remove the line directly below the Notebook Record header)
					if (XmlUtils.GetOptionalBooleanAttributeValue(slice.ConfigurationNode, "skipSpacerLine", false) &&
						slice is SummarySlice)
						continue;

					// Check for attribute that the next slice should be grouped with the current slice
					// regardless of whether they represent the same object.
					bool fSameObject = XmlUtils.GetOptionalBooleanAttributeValue(nextSlice.ConfigurationNode, "sameObject", false);

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
					else if (fSameObject ||
						nextSlice.Weight == ObjectWeight.light ||
						SameSourceObject(slice, nextSlice))
					{
						xPos = SliceSplitPositionBase + Math.Min(slice.LabelIndent(), nextSlice.LabelIndent());
					}
					gr.DrawLine(linePen, xPos, yPos, width, yPos);
				}
			}
		}
		}

		/// <summary>
		/// Return the container control to which nested controls belonging to slices should be added.
		/// This is the main DataTreeDiagram if not using a splitter, and the extra right-
		/// hand pane if using one.
		/// </summary>
		public UserControl SliceControlContainer
		{
			get
			{
				CheckDisposed();
				return this;
			}
		}

		/// <summary></summary>
		public FdoCache Cache
		{
			get
			{
				CheckDisposed();
				return m_cache;
			}
		}

		/// <summary>
		/// Create slices for the specified object by finding a relevant template in the spec.
		/// </summary>
		/// <param name="obj">The object to make slices for.</param>
		/// <param name="parentSlice">The parent slice.</param>
		/// <param name="layoutName">Name of the layout.</param>
		/// <param name="layoutChoiceField">The layout choice field.</param>
		/// <param name="indent">The indent.</param>
		/// <param name="insertPosition">The insert position.</param>
		/// <param name="path">sequence of nodes and HVOs inside which this is nested</param>
		/// <param name="reuseMap">map of key/slice combinations from a DataTree being refreshed. Exact matches may be
		/// reused, and also, the expansion state of exact matches is preserved.</param>
		/// <param name="unifyWith">If not null, this is a node to be 'unified' with the one looked up
		/// using the layout name.</param>
		/// <returns>
		/// updated insertPosition for next item after the ones inserted.
		/// </returns>
		public virtual int CreateSlicesFor(ICmObject obj, Slice parentSlice, string layoutName, string layoutChoiceField, int indent,
			int insertPosition, ArrayList path, ObjSeqHashMap reuseMap, XmlNode unifyWith)
		{
			CheckDisposed();

			// NB: 'path' can hold either ints or XmlNodes, so a generic can't be used for it.
			if (obj == null)
				return insertPosition;
			XmlNode template = GetTemplateForObjLayout(obj, layoutName, layoutChoiceField);
			path.Add(template);
			XmlNode template2 = template;
			if (unifyWith != null && unifyWith.ChildNodes.Count > 0)
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
		private XmlNode GetTemplateForObjLayout(ICmObject obj, string layoutName,
			string layoutChoiceField)
		{
			int classId = obj.ClassID;
			string choiceGuidStr = null;
			if (!string.IsNullOrEmpty(layoutChoiceField))
			{
				int flid = m_cache.MetaDataCacheAccessor.GetFieldId2(obj.ClassID, layoutChoiceField, true);
				m_monitoredProps.Add(Tuple.Create(obj.Hvo, flid));
				int hvo = m_cache.DomainDataByFlid.get_ObjectProp(obj.Hvo, flid);
				if (hvo != 0)
					choiceGuidStr = m_cache.ServiceLocator.GetObject(hvo).Guid.ToString();
			}

			//Custom Lists can have different selections of writing systems. LT-11941
			if (m_mdc.GetClassName(classId) == "CmCustomItem")
			{
				var owningList = (obj as ICmPossibility).OwningList;
				if (owningList == null)
					layoutName = "CmPossibilityA"; // As good a default as any
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

			XmlNode template;
			string useName = layoutName ?? "default";
			string origName = useName;
			for( ; ; )
			{
				string classname = m_mdc.GetClassName(classId);
				// Inventory of layouts has keys class, type, name
				template = m_layoutInventory.GetElement("layout", new[] {classname, "detail", useName, choiceGuidStr});
				if (template != null)
					break;
				if (obj is IRnGenericRec)
				{
					// New custom type, so we need to get the default template and add the new type. See FWR-1049
					template = m_layoutInventory.GetElement("layout", new[] { classname, "detail", useName, null });
					if (template != null)
					{
						XmlNode newTemplate = template.Clone();
						XmlUtils.AppendAttribute(newTemplate, "choiceGuid", choiceGuidStr);
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
					throw new ApplicationException("No matching layout found for class " + classname + " detail layout " + origName);
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
		/// <param name="mdc"></param>
		/// <param name="stClassName"></param>
		/// <returns>ClassId, or 0 if not found.</returns>
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
			// which has little hope of matching that fixed orginal key, won't it.
			// I can see how it would work when a simple F4 refresh is being done,
			// since the count of slices should remain the same.

			IList list = reuseMap[path];
			if (list.Count > 0)
			{
				var slice = (Slice)list[0];
				reuseMap.Remove(path, slice);
				return slice;
			}

			return null;
		}
		public enum NodeTestResult
		{
			kntrSomething, // really something here we could expand
			kntrPossible, // nothing here, but there could be
			kntrNothing // nothing could possibly be here, don't show collapsed OR expanded.
		}

		/// <summary>
		/// Apply a layout to an object, producing the specified slices.
		/// </summary>
		/// <param name="obj">The object we want a detai view of</param>
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
		public int ApplyLayout(ICmObject obj, Slice parentSlice, XmlNode template, int indent, int insertPosition,
			ArrayList path, ObjSeqHashMap reuseMap)
		{
			CheckDisposed();
			NodeTestResult ntr;
			return ApplyLayout(obj, parentSlice, template, indent, insertPosition, path, reuseMap, false, out ntr);
		}

		/// <summary>
		/// This is the guts of ApplyLayout, but it has extra arguments to allow it to be used both to actually produce
		/// slices, and just to query whether any slices will be produced.
		/// </summary>
		/// <param name="obj">The obj.</param>
		/// <param name="parentSlice">The parent slice.</param>
		/// <param name="template">The template.</param>
		/// <param name="indent">The indent.</param>
		/// <param name="insertPosition">The insert position.</param>
		/// <param name="path">The path.</param>
		/// <param name="reuseMap">The reuse map.</param>
		/// <param name="isTestOnly">if set to <c>true</c> [is test only].</param>
		/// <param name="testResult">The test result.</param>
		protected internal virtual int ApplyLayout(ICmObject obj, Slice parentSlice, XmlNode template, int indent, int insertPosition,
			ArrayList path, ObjSeqHashMap reuseMap, bool isTestOnly, out NodeTestResult testResult)
		{
			int insPos = insertPosition;
			testResult = NodeTestResult.kntrNothing;
			int cPossible = 0;
			// This loop handles the multiple parts of a layout.
			foreach (XmlNode partRef in template.ChildNodes)
			{
				if (partRef.GetType() == typeof(XmlComment))
					continue;

				// This code looks for the a special part definition with an attribute called "customFields"
				// It doesn't matter what this attribute is set to, as long as it exists.  If this attribute is
				// found, the custom fields will not be generated.
				if (XmlUtils.GetOptionalAttributeValue(partRef, "customFields", null) != null)
				{
					if(!isTestOnly)
						EnsureCustomFields(obj, template, partRef);

					continue;
				}

				testResult = ProcessPartRefNode(partRef, path, reuseMap, obj, parentSlice, indent, ref insPos, isTestOnly);

				if (isTestOnly)
				{
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
			}

			if (cPossible > 0)
				testResult = NodeTestResult.kntrPossible;	// everything else was nothing...

			//TODO: currently, we are making a custom fields show up all over the place... i.e.,
			//	the initial algorithm here (show the custom fields for a class whenever we are applying a template of that class)
			//		has turned out to be too simplistic, since apparently we and templates of a given class multiple times
			//		to show different parts of the class.
			//			if(template.Name == "template")
			//if (fGenerateCustomFields)
			//	testResult = AddCustomFields(obj, template, indent, ref insPos, path, reuseMap,isTestOnly);

			return insPos;
		}

		/// <summary>
		/// Process a top-level child of a layout (other than a comment).
		/// Currently these are part nodes (with ref= indicating the part to use) and sublayout nodes.
		/// </summary>
		/// <param name="partRef">The part ref.</param>
		/// <param name="path">The path.</param>
		/// <param name="reuseMap">The reuse map.</param>
		/// <param name="obj">The obj.</param>
		/// <param name="parentSlice">The parent slice.</param>
		/// <param name="indent">The indent.</param>
		/// <param name="insPos">The ins pos.</param>
		/// <param name="isTestOnly">if set to <c>true</c> [is test only].</param>
		/// <returns>NodeTestResult</returns>
		private NodeTestResult ProcessPartRefNode(XmlNode partRef, ArrayList path, ObjSeqHashMap reuseMap,
			ICmObject obj, Slice parentSlice, int indent, ref int insPos, bool isTestOnly)
		{
			NodeTestResult ntr = NodeTestResult.kntrNothing;
			switch (partRef.Name)
			{
				case "sublayout":
					// a sublayout simply includes another layout within the current layout, the layout is
					// located by name and choice field
					string layoutName = XmlUtils.GetOptionalAttributeValue(partRef, "name");
					string layoutChoiceField = XmlUtils.GetOptionalAttributeValue(partRef, "layoutChoiceField");
					XmlNode template = GetTemplateForObjLayout(obj, layoutName, layoutChoiceField);
					path.Add(partRef);
					path.Add(template);
					insPos = ApplyLayout(obj, parentSlice, template, indent, insPos, path, reuseMap, isTestOnly, out ntr);
					path.RemoveAt(path.Count - 1);
					path.RemoveAt(path.Count - 1);
					break;

				case "part":
					// If the previously selected slice doesn't display in this refresh, we try for the next
					// visible slice instead.  So m_fSetCurrentSliceNew might still be set.  See LT-9010.
					string partName = XmlUtils.GetManditoryAttributeValue(partRef, "ref");
					if (!m_fSetCurrentSliceNew && m_currentSlicePartName != null && obj.Guid == m_currentSliceObjGuid)
					{
						for (int clid = obj.ClassID; clid != 0; clid = m_mdc.GetBaseClsId(clid))
						{
							string sFullPartName = String.Format("{0}-Detail-{1}", m_mdc.GetClassName(clid), partName);
							if (m_currentSlicePartName == sFullPartName)
							{
								m_fSetCurrentSliceNew = true;
								break;
							}
						}
					}
					string visibility = "always";
					if (!m_fShowAllFields)
					{
						visibility = XmlUtils.GetOptionalAttributeValue(partRef, "visibility", "always");
						if (visibility == "never")
							return NodeTestResult.kntrNothing;
						Debug.Assert(visibility == "always" || visibility == "ifdata");
					}

					// Use the part inventory to find the indicated part.
					int classId = obj.ClassID;
					XmlNode part;
					for (;;)
					{
						string classname = m_mdc.GetClassName(classId);
						// Inventory of parts has key ID. The ID is made up of the class name, "-Detail-", partname.
						string key = classname + "-Detail-" + partName;
						part = m_partInventory.GetElement("part", new[] {key});

						if (part != null)
							break;
						if (classId == 0) // we've just tried CmObject.
						{
							Debug.WriteLine("Warning: No matching part found for " + classname + "-Detail-" + partName);
							// Just omit the missing part.
							return NodeTestResult.kntrNothing;
						}
						// Otherwise try superclass.
						classId = m_mdc.GetBaseClsId(classId);
					}
					string parameter = XmlUtils.GetOptionalAttributeValue(partRef, "param", null);
					// If you are wondering why we put the partref in the key, one reason is that it may be needed
					// when expanding a collapsed slice.
					path.Add(partRef);
					ntr = ProcessPartChildren(part, path, reuseMap, obj, parentSlice, indent, ref insPos, isTestOnly,
						parameter, visibility == "ifdata", partRef);
					path.RemoveAt(path.Count - 1);
					break;
			}
			return ntr;
		}

		internal NodeTestResult ProcessPartChildren(XmlNode part, ArrayList path,
			ObjSeqHashMap reuseMap, ICmObject obj, Slice parentSlice, int indent, ref int insPos, bool isTestOnly,
			string parameter, bool fVisIfData, XmlNode caller)
		{
			CheckDisposed();
			// The children of the part element must now be processed. Often there is only one.
			foreach (XmlNode node in part.ChildNodes)
			{
				if (node.GetType() == typeof(XmlComment))
					continue;
				NodeTestResult testResult = ProcessSubpartNode(node, path, reuseMap, obj, parentSlice,
					indent, ref insPos, isTestOnly, parameter, fVisIfData, caller);
				// If we're just looking to see if there would be any slices, and there was,
				// then don't bother thinking about any more slices.
				if (isTestOnly && testResult != NodeTestResult.kntrNothing)
					return testResult;
			}
			return NodeTestResult.kntrNothing; // valid if isTestOnly, otherwise don't care.
		}

		/// <summary>
		/// Append to the part refs of template a suitable one for each custom field of
		/// the class of obj.
		/// </summary>
		/// <param name="obj">The obj.</param>
		/// <param name="template">The template.</param>
		/// <param name="insertAfter">The insert after.</param>
		private void EnsureCustomFields(ICmObject obj, XmlNode template, XmlNode insertAfter)
		{
			var interestingClasses = new Set<int>();
			int clsid = obj.ClassID;
			while (clsid != 0)
			{
				interestingClasses.Add(clsid);
				clsid = obj.Cache.MetaDataCacheAccessor.GetBaseClsId(clsid);
			}
			//for each custom field, we need to construct or reuse the kind of XmlNode that would normally be found in the XDE file
			foreach (FieldDescription field in FieldDescription.FieldDescriptors(m_cache))
			{
				if (field.IsCustomField && interestingClasses.Contains(field.Class))
				{
					bool exists = false;
					string target = field.Name;
					// We could do this search with an XPath but they are excruciatingly slow.
					// Check all of the siblings, first going forward, then backward
					for(XmlNode sibling = insertAfter.NextSibling; sibling != null && !exists;	sibling = sibling.NextSibling)
					{
						if (CheckCustomFieldsSibling(sibling, target))
							exists = true;
					}
					for(XmlNode sibling = insertAfter.PreviousSibling; sibling != null && !exists;	sibling = sibling.PreviousSibling)
					{
						if (CheckCustomFieldsSibling(sibling, target))
							exists = true;
					}

					if (exists)
						continue;

					XmlNode part = template.OwnerDocument.CreateElement("part");
					AddAttribute(part, "ref", "Custom");
					AddAttribute(part, "param", target);
					template.InsertAfter(part, insertAfter);
				}
			}
		}

		private static bool CheckCustomFieldsSibling(XmlNode sibling, string target)
		{
			if (sibling.Attributes == null)
				return false;	// no attributes on this nodeas XmlComment  LT-3566

			XmlNode paramAttr = sibling.Attributes["param"];
			XmlNode refAttr = sibling.Attributes["ref"];
			if (paramAttr != null && refAttr != null && paramAttr.Value == target && sibling.Name == "part" && refAttr.Value == "Custom")
				return true;

			return false;
		}

		private static void AddAttribute(XmlNode node, string name, string value)
		{
			XmlAttribute attribute = node.OwnerDocument.CreateAttribute(name);
			attribute.Value = value;
			node.Attributes.Append(attribute);
		}

		/// <summary>
		/// Handle one (non-comment) child node of a template (or other node) being used to
		/// create slices.  Update insertPosition to indicate how many were added (it also
		/// specifies where to add).  If fTestOnly is true, do not update insertPosition, just
		/// return true if any slices would be created.  Note that this method is recursive
		/// indirectly through ProcessPartChildren().
		/// </summary>
		/// <param name="node">The node.</param>
		/// <param name="path">The path.</param>
		/// <param name="reuseMap">The reuse map.</param>
		/// <param name="obj">The obj.</param>
		/// <param name="parentSlice">The parent slice.</param>
		/// <param name="indent">The indent.</param>
		/// <param name="insertPosition">The insert position.</param>
		/// <param name="fTestOnly">if set to <c>true</c> [f test only].</param>
		/// <param name="parameter">The parameter.</param>
		/// <param name="fVisIfData">If true, show slice only if data present.</param>
		/// <param name="caller">The caller.</param>
		private NodeTestResult ProcessSubpartNode(XmlNode node, ArrayList path,
			ObjSeqHashMap reuseMap, ICmObject obj, Slice parentSlice, int indent, ref int insertPosition,
			bool fTestOnly, string parameter, bool fVisIfData,
		XmlNode caller)
		{
			string editor = XmlUtils.GetOptionalAttributeValue(node, "editor");

			try
			{
				if (editor != null)
					editor = editor.ToLower();
				int flid = GetFlidFromNode(node, obj);

				if (m_sliceFilter != null &&
					flid != 0 &&
					!m_sliceFilter.IncludeSlice(node, obj, flid))
				{
					return NodeTestResult.kntrNothing;
				}

				switch (node.Name)
				{
					default:
						break;
					// Nothing to do for unrecognized element, such as deParams.

					case "slice":
						return AddSimpleNode(path, node, reuseMap, editor, flid, obj, parentSlice, indent,
							ref insertPosition, fTestOnly,
						fVisIfData, caller);

					case "seq":
						return AddSeqNode(path, node, reuseMap, flid, obj, parentSlice, indent + Slice.ExtraIndent(node),
							ref insertPosition, fTestOnly, parameter,
						fVisIfData, caller);

					case "obj":
						return AddAtomicNode(path, node, reuseMap, flid, obj, parentSlice, indent  + Slice.ExtraIndent(node),
							ref insertPosition, fTestOnly, parameter,
						fVisIfData, caller);

					case "if":
						if (XmlVc.ConditionPasses(node, obj.Hvo, m_cache))
						{
							NodeTestResult ntr = ProcessPartChildren(node, path, reuseMap, obj, parentSlice,
								indent, ref insertPosition, fTestOnly, parameter, fVisIfData,
								caller);
							if (fTestOnly && ntr != NodeTestResult.kntrNothing)
								return ntr;
						}
						break;

					case "ifnot":
						if (!XmlVc.ConditionPasses(node, obj.Hvo, m_cache))
						{
							NodeTestResult ntr = ProcessPartChildren(node, path, reuseMap, obj, parentSlice,
								indent, ref insertPosition, fTestOnly, parameter, fVisIfData,
								caller);
							if (fTestOnly && ntr != NodeTestResult.kntrNothing)
								return ntr;
						}
						break;

					case "choice":
						foreach (XmlNode clause in node.ChildNodes)
						{
							if (clause.Name == "where")
							{
								if (XmlVc.ConditionPasses(clause, obj.Hvo, m_cache))
								{
									NodeTestResult ntr = ProcessPartChildren(clause, path,
										reuseMap, obj, parentSlice, indent, ref insertPosition, fTestOnly,
										parameter, fVisIfData,
									caller);
									if (fTestOnly && ntr != NodeTestResult.kntrNothing)
										return ntr;
									break;
								}
								// Allow multiple where elements to be processed, but expand only
								// the first one whose condition passes.
							}
							else if (clause.Name == "otherwise")
							{
								// enhance: verify last node?
								NodeTestResult ntr = ProcessPartChildren(clause, path,
									reuseMap, obj, parentSlice, indent, ref insertPosition, fTestOnly,
									parameter, fVisIfData,
								caller);
								if (fTestOnly && ntr != NodeTestResult.kntrNothing)
									return ntr;
								break;
							}
							else
							{
								throw new Exception(
									"elements in choice must be <where...> or <otherwise>.");
							}
						}
						break;

					case "RecordChangeHandler":
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
						m_listName = XmlUtils.GetOptionalAttributeValue(node,
							"listName");
						m_rlu = null;
						ResetRecordListUpdater();
						// m_rlu may still be null, but that appears to be just fine.
						m_rch.Setup(obj, m_rlu);
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
					bldr.AppendLine(" The editor was '" + editor + "'.");
				bldr.Append(" The text of the current node was " + node.OuterXml);
				//now send it on
				throw new ApplicationException(bldr.ToString(), error);
			}
			// other types of child nodes, for example, parameters for jtview, don't even have
			// the potential for expansion.
			return NodeTestResult.kntrNothing;
		}

		void m_rch_Disposed(object sender, EventArgs e)
		{
			// It was disposed, so clear out the data member.
			if (m_rch != null)
				m_rch.Disposed -= m_rch_Disposed;
			m_rch = null;
		}

		private int GetFlidFromNode(XmlNode node, ICmObject obj)
		{
			string attrName = XmlUtils.GetOptionalAttributeValue(node, "field");
			if ((node.Name == "if" || node.Name == "ifnot") &&
				(XmlUtils.GetOptionalAttributeValue(node, "target", "this").ToLower() != "this" ||
				(attrName != null && attrName.IndexOf('/') != -1)))
			{
				// Can't get the field value for a target other than "this", or a field that does
				// not belong directly to "this".
				return 0;
			}
			int flid = 0;
			if (attrName != null)
			{
				try
				{
					flid = m_mdc.GetFieldId2(obj.ClassID, attrName, true);
				}
				catch
				{
					throw new ApplicationException(
						"DataTree could not find the flid for attribute '" + attrName +
						"' of class '" + obj.ClassID + "'.");
				}
			}
			return flid;
		}

		private NodeTestResult AddAtomicNode(ArrayList path, XmlNode node, ObjSeqHashMap reuseMap, int flid,
			ICmObject obj, Slice parentSlice, int indent, ref int insertPosition, bool fTestOnly, string layoutName,
			bool fVisIfData, XmlNode caller)
		{
			// Facilitate insertion of an expandable tree node representing an owned or ref'd object.
			if (flid == 0)
				throw new ApplicationException("field attribute required for atomic properties " + node.OuterXml);
			var innerObj = m_cache.GetAtomicPropObject(m_cache.DomainDataByFlid.get_ObjectProp(obj.Hvo, flid));
			m_monitoredProps.Add(Tuple.Create(obj.Hvo, flid));
			if (fVisIfData && innerObj == null)
				return NodeTestResult.kntrNothing;
			if (fTestOnly)
			{
				if (innerObj != null || XmlUtils.GetOptionalAttributeValue(node, "ghost") != null)
					return NodeTestResult.kntrSomething;

				return NodeTestResult.kntrPossible;
			}
			path.Add(node);
			if (innerObj != null)
			{
				string layoutOverride = XmlUtils.GetOptionalAttributeValue(node, "layout", layoutName);
				string layoutChoiceField = XmlUtils.GetOptionalAttributeValue(node, "layoutChoiceField");
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

		void MakeGhostSlice(ArrayList path, XmlNode node, ObjSeqHashMap reuseMap, ICmObject obj, Slice parentSlice,
			int flidEmptyProp, XmlNode caller, int indent, ref int insertPosition)
		{
			// It's a really bad idea to add it to the path, since it kills
			// the code that hot swaps it, when becoming real.
			//path.Add(node);
			if (parentSlice != null)
				Debug.Assert(!parentSlice.IsDisposed, "AddSimpleNode parameter 'parentSlice' is Disposed!");
			Slice slice = GetMatchingSlice(path, reuseMap);
			if (slice == null)
			{
				slice = new GhostStringSlice(obj, flidEmptyProp, node, m_cache);
				// Set the label and abbreviation (in that order...abbr defaults to label if not given.
				// Note that we don't have a "caller" here, so we pass 'node' as both arguments...
				// means it gets searched twice if not found, but that's fairly harmless.
				slice.Label = GetLabel(node, node, obj, "ghostLabel");
				slice.Abbreviation = GetLabelAbbr(node, node, obj, slice.Label, "ghostAbbr");

				// Install new item at appropriate position and level.
				slice.Indent = indent;
				slice.Object = obj;
				slice.Cache = m_cache;
				slice.Mediator = m_mediator;


				// We need a copy since we continue to modify path, so make it as compact as possible.
				slice.Key = path.ToArray();
				slice.ConfigurationNode = node;
				slice.CallerNode = caller;
				// don't mess with this, the obj/seq node would not have a meaningful back color override
				// for the slice. If we need it invent a new attribute.
				//slice.OverrideBackColor(XmlUtils.GetOptionalAttributeValue(node, "backColor"));

				// dubious...should the string slice really get the context menu for the object?
				slice.ShowContextMenu += OnShowContextMenu;

				slice.SmallImages = SmallImages;
				SetNodeWeight(node, slice);

				slice.FinishInit();
				// Now done in Slice.ctor
				//slice.Visible = false; // don't show it until we position and size it.
				InsertSliceAndRegisterWithContextHelp(insertPosition, slice);
			}
			else
			{
				EnsureValidIndexForReusedSlice(slice, insertPosition);
			}
			slice.ParentSlice = parentSlice;
			insertPosition++;
			// Since we didn't add it to the path,
			// then there is nothign to do at this end either..
			//slice.GenerateChildren(node, caller, obj, indent, ref insertPosition, path, reuseMap);
			//path.RemoveAt(path.Count - 1);
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
		/// <param name="hvoOwner"></param>
		/// <param name="flidOwning"></param>
		/// <remarks>Owning Flids may not be enough.  We may need reference flids as well.
		/// This is tricky, since this is called where the slice itself isn't known.</remarks>
		internal void SetCurrentObjectFlids(int hvoOwner, int flidOwning)
		{
			m_currentObjectFlids.Clear();
			if (flidOwning != 0)
				m_currentObjectFlids.Add(flidOwning);
			for (int hvo = hvoOwner; hvo != 0;
				hvo = m_cache.ServiceLocator.GetObject(hvo).Owner == null
				? 0 : m_cache.ServiceLocator.GetObject(hvo).Owner.Hvo)
			{
				var flid = m_cache.ServiceLocator.GetObject(hvo).OwningFlid;
				if (flid != 0)
					m_currentObjectFlids.Add(flid);
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

		private NodeTestResult AddSeqNode(ArrayList path, XmlNode node, ObjSeqHashMap reuseMap, int flid,
			ICmObject obj, Slice parentSlice, int indent, ref int insertPosition, bool fTestOnly, string layoutName,
			bool fVisIfData, XmlNode caller)
		{
			if (flid == 0)
				throw new ApplicationException("field attribute required for seq properties " + node.OuterXml);
			int cobj = m_cache.DomainDataByFlid.get_VecSize(obj.Hvo, flid);
			// monitor it even if we're testing: result may change.
			m_monitoredProps.Add(Tuple.Create(obj.Hvo, flid));
			if (fVisIfData && cobj == 0)
				return NodeTestResult.kntrNothing;
			if (fTestOnly)
			{
				if (cobj > 0 || XmlUtils.GetOptionalAttributeValue(node, "ghost") != null)
					return NodeTestResult.kntrSomething;

				return NodeTestResult.kntrPossible;
			}
			path.Add(node);
			string layoutOverride = XmlUtils.GetOptionalAttributeValue(node, "layout", layoutName);
			string layoutChoiceField = XmlUtils.GetOptionalAttributeValue(node, "layoutChoiceField");
			if (cobj == 0)
			{
				// Nothing in seq....do we want a ghost slice?
				if (XmlUtils.GetOptionalAttributeValue(node, "ghost") != null)
				{
					MakeGhostSlice(path, node, reuseMap, obj, parentSlice, flid, caller, indent, ref insertPosition);
				}
			}
			else if (cobj < kInstantSliceMax ||	// This may be a little on the small side
				m_currentObjectFlids.Contains(flid) ||
				(!String.IsNullOrEmpty(m_currentSlicePartName) && m_currentSliceObjGuid != Guid.Empty && m_currentSliceNew == null))
			{
				//Create slices immediately
				var contents = SetupContents(flid, obj);
				foreach (int hvo in contents)
				{
					path.Add(hvo);
					insertPosition = CreateSlicesFor(m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo),
						parentSlice, layoutOverride, layoutChoiceField, indent, insertPosition, path, reuseMap, caller);
					path.RemoveAt(path.Count - 1);
				}
			}
			else
			{
				// Create unique DummyObjectSlices for each slice.  This may reduce the initial
				// preceived benefit, but this way doesn't crash now that the slices are being
				// disposed of.
				int cnt = 0;
				var contents = SetupContents(flid, obj);
				foreach (int hvo in contents)
				{
					// TODO (DamienD): do we need to add the layout choice field to the monitored props for a dummy slice?
					// LT-12302 exposed a path through here that was messed up when hvo was added before Dummy slices
					//path.Add(hvo); // try putting this AFTER the dos creation
					var dos = new DummyObjectSlice(indent, node, (ArrayList)(path.Clone()),
						obj, flid, cnt, layoutOverride, layoutChoiceField, caller) {Cache = m_cache, ParentSlice = parentSlice};
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
			int chvoMax = m_cache.DomainDataByFlid.get_VecSize(obj.Hvo, flid);
			using (ArrayPtr arrayPtr = MarshalEx.ArrayToNative<int>(chvoMax))
			{
				m_cache.DomainDataByFlid.VecProp(obj.Hvo, flid, chvoMax, out chvoMax, arrayPtr);
				contents = MarshalEx.NativeToArray<int>(arrayPtr, chvoMax);
			}
			return contents;
		}

		private readonly Set<string> m_setInvalidFields = new Set<string>();
		/// <summary>
		/// This seems a bit clumsy, but the metadata cache now throws an exception if the class
		/// id/field name pair isn't valid for GetFieldId2().  Limiting this to only one throw
		/// per class/field pair seems a reasonable compromise.  To avoid all throws would
		/// require duplicating much of the metadata cache locally.
		/// </summary>
		internal int GetFlidIfPossible(int clid, string fieldName, IFwMetaDataCacheManaged mdc)
		{
			string key = fieldName + clid;
			if (m_setInvalidFields.Contains(key))
				return 0;
			try
			{
				int flid = mdc.GetFieldId2(clid, fieldName, true);
				return flid;
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
		/// <param name="label">The label.</param>
		/// <param name="obj">The obj.</param>
		/// <returns></returns>
		internal string InterpretLabelAttribute(string label, ICmObject obj)
		{
			CheckDisposed();
			if (label != null && label.Length > 7 && label.Substring(0,7).ToLower() == "$owner.")
			{
				string subfield = label.Substring(7);
				var owner = obj.Owner;
				IFwMetaDataCache mdc = Cache.DomainDataByFlid.MetaDataCache;
				int flidSubfield = GetFlidIfPossible(owner.ClassID, subfield, mdc as IFwMetaDataCacheManaged);
				if (flidSubfield != 0)
				{
					var type = (CellarPropertyType)Cache.DomainDataByFlid.MetaDataCache.GetFieldType(flidSubfield);
					switch (type)
					{
					default:
						Debug.Assert(type == CellarPropertyType.Unicode);
						break;
					case CellarPropertyType.MultiString:
					case CellarPropertyType.MultiBigString:
						label = Cache.DomainDataByFlid.get_MultiStringAlt(owner.Hvo,
							flidSubfield,
							Cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle).Text;
						break;
					case CellarPropertyType.MultiUnicode:
					case CellarPropertyType.MultiBigUnicode:
						label = Cache.DomainDataByFlid.get_MultiStringAlt(owner.Hvo,
							flidSubfield,
							Cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle).Text;
						break;
					case CellarPropertyType.String:
					case CellarPropertyType.BigString:
						label = Cache.DomainDataByFlid.get_StringProp(owner.Hvo, flidSubfield).Text;
						break;
					case CellarPropertyType.Unicode:
					case CellarPropertyType.BigUnicode:
						label = Cache.DomainDataByFlid.get_UnicodeProp(owner.Hvo, flidSubfield);
						break;
					}
				}
			}
			return label;
		}

		/// <summary>
		/// Tests to see if it should add the field (IfData), then adds the field.
		/// </summary>
		/// <param name="path">The path.</param>
		/// <param name="node">The node.</param>
		/// <param name="reuseMap">The reuse map.</param>
		/// <param name="editor">Type of Contained Data</param>
		/// <param name="flid">Field ID</param>
		/// <param name="obj">The obj.</param>
		/// <param name="parentSlice">The parent slice.</param>
		/// <param name="indent">The indent.</param>
		/// <param name="insPos">The ins pos.</param>
		/// <param name="fTestOnly">if set to <c>true</c> [f test only].</param>
		/// <param name="fVisIfData">IfData</param>
		/// <param name="caller">The caller.</param>
		/// <returns>
		/// NodeTestResult, an enum showing if usable data is contained in the field
		/// </returns>
		private NodeTestResult AddSimpleNode(ArrayList path, XmlNode node, ObjSeqHashMap reuseMap, string editor,
			int flid, ICmObject obj, Slice parentSlice, int indent, ref int insPos, bool fTestOnly, bool fVisIfData, XmlNode caller)
		{
			var realSda = m_cache.DomainDataByFlid;
			if (parentSlice != null)
				Debug.Assert(!parentSlice.IsDisposed, "AddSimpleNode parameter 'parentSlice' is Disposed!");
			IWritingSystemContainer wsContainer = m_cache.ServiceLocator.WritingSystems;
			if (fVisIfData) // Contains the tests to see if usable data is inside the field (for all types of fields)
			{
				if (editor != null && editor == "custom")
				{
					Type typeFound;
					System.Reflection.MethodInfo mi =
						XmlUtils.GetStaticMethod(node, "assemblyPath", "class", "ShowSliceForVisibleIfData", out typeFound);
					if (mi != null)
					{
						var parameters = new object[2];
						parameters[0] = node;
						parameters[1] = obj;
						object result = mi.Invoke(typeFound,
							System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public |
							System.Reflection.BindingFlags.NonPublic, null, parameters, null);
						if (!(bool)result)
							return NodeTestResult.kntrNothing;
					}
				}
				else if (flid == 0 && editor != null && editor == "autocustom")
				{
					flid = SliceFactory.GetCustomFieldFlid(caller, realSda.MetaDataCache, obj);
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
						case CellarPropertyType.MultiBigString:
						case CellarPropertyType.MultiBigUnicode:
							string ws = XmlUtils.GetOptionalAttributeValue(node, "ws", null);
							switch (ws)
							{
								case "vernacular":
									if (realSda.get_MultiStringAlt(obj.Hvo, flid,
										wsContainer.DefaultVernacularWritingSystem.Handle).Length == 0)
										return NodeTestResult.kntrNothing;
									break;
								case "analysis":
									if (realSda.get_MultiStringAlt(obj.Hvo,
										flid,
										wsContainer.DefaultAnalysisWritingSystem.Handle).Length == 0)
										return NodeTestResult.kntrNothing;
									break;
								default:
									if (editor == "jtview")
									{
										if (realSda.get_MultiStringAlt(obj.Hvo,
											flid,
											wsContainer.DefaultAnalysisWritingSystem.Handle).Length == 0)
											return NodeTestResult.kntrNothing;
									}
									// try one of the magic ones for multistring
									int wsMagic = WritingSystemServices.GetMagicWsIdFromName(ws);
									if (wsMagic == 0 && editor == "autocustom")
									{
										wsMagic = realSda.MetaDataCache.GetFieldWs(flid);
									}
									if (wsMagic == 0 && editor != "autocustom")
										break; // not recognized, treat as visible
									var rgws = WritingSystemServices.GetWritingSystemList(m_cache, wsMagic, false).ToArray();
									bool anyNonEmpty = false;
									foreach (IWritingSystem wsInst in rgws)
									{
										if (realSda.get_MultiStringAlt(obj.Hvo, flid, wsInst.Handle).Length != 0)
										{
											anyNonEmpty = true;
											break;
										}
									}
									if (!anyNonEmpty)
										return NodeTestResult.kntrNothing;
									break;
							}
							break;
						case CellarPropertyType.String:
						case CellarPropertyType.BigString:
							if (realSda.get_StringProp(obj.Hvo, flid).Length == 0)
								return NodeTestResult.kntrNothing;
							break;
						case CellarPropertyType.Unicode:
						case CellarPropertyType.BigUnicode:
							string val = realSda.get_UnicodeProp(obj.Hvo, flid);
							if (string.IsNullOrEmpty(val))
								return NodeTestResult.kntrNothing;
							break;
							// Usually, the header nodes for sequences and atomic object props
							// have no editor. But sometimes they may have a jtview summary
							// or the like. If an object-prop flid is specified, check it,
							// in case we want to suppress the whole header.
						case CellarPropertyType.OwningAtomic:
						case CellarPropertyType.ReferenceAtomic:
							int hvoT = realSda.get_ObjectProp(obj.Hvo, flid);
							if (hvoT == 0)
								return NodeTestResult.kntrNothing;
							var objt = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoT);
							if (objt.ClassID == StTextTags.kClassId) // if clid is an sttext clid
							{
								var txt = (IStText) objt;
								// Test if the StText has only one paragraph
								int cpara = txt.ParagraphsOS.Count;
								if (cpara == 1)
								{
									// Tests if paragraph is empty
									ITsString tss = ((IStTxtPara) txt.ParagraphsOS[0]).Contents;
									if (tss == null || tss.Length == 0)
										return NodeTestResult.kntrNothing;
								}
							}
							break;
						case CellarPropertyType.ReferenceCollection:
							// Currently this special case is only needed for ReferenceCollection (specifically for PublishIn).
							// We can broaden it if necessary, but why take the time to look for it elsewhere?
							int visibilityFlid = flid;
							string visField = XmlUtils.GetOptionalAttributeValue(node, "visField");
							if (visField != null)
							{
								int clsid = Cache.MetaDataCacheAccessor.GetOwnClsId(flid);
								visibilityFlid = Cache.MetaDataCacheAccessor.GetFieldId2(clsid, visField, true);
							}
							if (realSda.get_VecSize(obj.Hvo, visibilityFlid) == 0)
								return NodeTestResult.kntrNothing;
							break;
						case CellarPropertyType.OwningCollection:
						case CellarPropertyType.OwningSequence:

						case CellarPropertyType.ReferenceSequence:
							if (realSda.get_VecSize(obj.Hvo, flid) == 0)
								return NodeTestResult.kntrNothing;
							break;
					}
				}
				else if (editor == null)
				{
					// may be a summary node for a sequence or atomic node. Suppress it as well as the prop.
					XmlNode child = null;
					int cnodes = 0;
					foreach (XmlNode n in node.ChildNodes)
					{
						if (node is XmlComment)
							continue;
						cnodes++;
						if (cnodes > 1)
							break;
						child = n;
					}
					if (child != null && cnodes == 1) // exactly one non-comment child
					{
						int flidChild = GetFlidFromNode(child, obj);
						// If it's an obj or seq node and the property is empty, we'll show nothing.
						if (flidChild != 0)
						{
							if ((child.Name == "seq" || child.Name == "obj")
								&& realSda.get_VecSize(obj.Hvo, flidChild) == 0)
							{
								return NodeTestResult.kntrNothing;
							}
						}
					}
				}
			}
			if (fTestOnly)
				return NodeTestResult.kntrSomething; // slices always produce something.

			path.Add(node);
			Slice slice = GetMatchingSlice(path, reuseMap);
			if (slice == null)
			{
				slice = SliceFactory.Create(m_cache, editor, flid, node, obj, StringTbl, PersistenceProvder, m_mediator, caller, reuseMap);
				if (slice == null)
				{
					// One way this can happen in TestLangProj is with a part ref for a custom field that
					// has been deleted.
					return NodeTestResult.kntrNothing;
				}
				Debug.Assert(slice != null);
				// Set the label and abbreviation (in that order...abbr defaults to label if not given
				if (slice.Label == null)
					slice.Label = GetLabel(caller, node, obj, "label");
				slice.Abbreviation = GetLabelAbbr(caller, node, obj, slice.Label, "abbr");

				// Install new item at appropriate position and level.
				slice.Indent = indent;
				slice.Object = obj;
				slice.Cache = m_cache;
				slice.StringTbl = StringTbl;
				slice.PersistenceProvider = PersistenceProvder;

				// We need a copy since we continue to modify path, so make it as compact as possible.
				slice.Key = path.ToArray();
				// old code just set mediator, nothing ever set m_configurationParams. Maybe the two are redundant and should merge?
				slice.Init(m_mediator, null);
				slice.ConfigurationNode = node;
				slice.CallerNode = caller;
				slice.OverrideBackColor(XmlUtils.GetOptionalAttributeValue(node, "backColor"));
				slice.ShowContextMenu += OnShowContextMenu;
				slice.SmallImages = SmallImages;
				SetNodeWeight(node, slice);

				slice.FinishInit();
				// Now done in Slice.ctor
				//slice.Visible = false; // don't show it until we position and size it.

				InsertSliceAndRegisterWithContextHelp(insPos, slice);
			}
			else
			{
				// Now done in Slice.ctor
				//slice.Visible = false; // Since some slices are invisible, all must be, or Show() will reorder them.
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
		/// <param name="slice"></param>
		/// <param name="insertPosition"></param>
		private void EnsureValidIndexForReusedSlice(Slice slice, int insertPosition)
		{
			int reusedSliceIdx = slice.IndexInContainer;
			if (insertPosition != reusedSliceIdx)
			{
				ForceSliceIndex(slice, insertPosition);
			}
			Debug.Assert(slice.IndexInContainer == insertPosition, String.Format("EnsureValideIndexFOrReusedSlice: slice '{0}' at index({1}) should have been inserted in index({2})",
				slice.ConfigurationNode.OuterXml, slice.IndexInContainer, insertPosition));
			ResetTabIndices(insertPosition);
		}

		/// <summary>
		/// Get a label-like attribute for the slice.
		/// </summary>
		/// <param name="caller"></param>
		/// <param name="node"></param>
		/// <param name="obj"></param>
		/// <param name="attr"></param>
		private string GetLabel(XmlNode caller, XmlNode node, ICmObject obj, string attr)
		{
			string label;
			if (Mediator != null && Mediator.HasStringTable)
			{
				label = XmlUtils.GetLocalizedAttributeValue(Mediator.StringTbl, caller, attr, null) ??
						XmlUtils.GetLocalizedAttributeValue(Mediator.StringTbl, node, attr, null);
			}
			else
			{
				label = XmlUtils.GetOptionalAttributeValue(caller, attr) ?? XmlUtils.GetOptionalAttributeValue(node, attr);
			}
			return InterpretLabelAttribute(label, obj);
		}

		/// <summary>
		/// Find a suitable abbreviation for the given label.
		/// </summary>
		/// <param name="caller"></param>
		/// <param name="node"></param>
		/// <param name="obj"></param>
		/// <param name="label"></param>
		/// <param name="attr"></param>
		/// <returns>null if no suitable abbreviation is found.</returns>
		private string GetLabelAbbr(XmlNode caller, XmlNode node, ICmObject obj, string label, string attr)
		{
			// First see if we can find an explicit attribute value.
			string abbr = GetLabel(caller, node, obj, attr);
			if (abbr != null)
				return abbr;

			// Otherwise, see if we can map the label to an abbreviation in the StringTable
			if (label != null && StringTbl != null)
			{
				abbr = StringTbl.GetString(label, "LabelAbbreviations");
				if (abbr == "*" + label + "*")
					abbr = null;	// couldn't find it in the StringTable, reset it to null.
			}
			abbr = InterpretLabelAttribute(abbr, obj);
			// NOTE: Currently, Slice.Abbreviation Property sets itself to a 4-char truncation of Slice.Label
			// internally when setting the property to null.  So, allow abbr == null, and let that code handle
			// the truncation.
			return abbr;
		}

		private static void SetNodeWeight(XmlNode node, Slice slice)
		{
			string weightString = XmlUtils.GetOptionalAttributeValue(node, "weight", "field");
			ObjectWeight weight;
			switch(weightString)
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
					throw new ConfigurationException("Invalid 'weight' value, should be heavy, normal, light, or field");
			}
			slice.Weight = weight;
		}

		/// <summary>
		/// Get the context menu that would be displayed for a right click on the slice.
		/// </summary>
		/// <param name="slice">The slice.</param>
		/// <param name="fHotLinkOnly">if set to <c>true</c> [f hot link only].</param>
		/// <returns></returns>
		public ContextMenu GetSliceContextMenu(Slice slice, bool fHotLinkOnly)
		{
			CheckDisposed();
			Debug.Assert(ShowContextMenuEvent!= null, "this should always be set to something");
			// This is something of a historical artifact. There's probably no reason
			// to pass a point to ShowContextMenuEvent. At an earlier stage, the event was
			// ShowContextMenu, so it needed a point. TreeNodeEventArgs is still used for
			// Slice.ShowContextMenu event, so it was somewhat awkward to change.
			var e = new SliceMenuRequestArgs(slice, fHotLinkOnly);
			return ShowContextMenuEvent(this, e);
		}

		///// <summary>
		///// this is called by a client which normally provides its own custom menu, in order to allow it to
		///// fall back on an auto menu during development, before the custom menu has been defined.
		///// </summary>
		///// <param name="sender"></param>
		///// <param name="e"></param>
		//		public ContextMenu GetAutoMenu (object sender, SIL.FieldWorks.Common.Framework.DetailControls.SliceMenuRequestArgs e)
		//		{
		//			return m_autoHandler.GetSliceContextMenu(sender, e);
		//		}

		/// <summary>
		/// Set the handler which will be invoked when the user right-clicks on the
		/// TreeNode portion of a slice, or for some other reason we need the context menu.
		/// </summary>
		/// <param name="handler"></param>
		public void SetContextMenuHandler(SliceShowMenuRequestHandler handler)
		{
			CheckDisposed();
			//note the = instead of += we do not want more than 1 handler trying to open the context menu!
			//you could try changing this if we wanted to have a fall back handler, and if there
			//was some way to get the first handler to be able to say "don't pass on this message"
			//when it handled the menu display itself.
			ShowContextMenuEvent = handler;
		}

		/// <summary>
		/// Calls ApplyLayout for each child of the argument node.
		/// </summary>
		/// <param name="obj">The obj.</param>
		/// <param name="parentSlice">The parent slice.</param>
		/// <param name="template">The template.</param>
		/// <param name="indent">The indent.</param>
		/// <param name="insertPosition">The insert position.</param>
		/// <param name="path">The path.</param>
		/// <param name="reuseMap">The reuse map.</param>
		/// <returns></returns>
		public int ApplyChildren(ICmObject obj, Slice parentSlice, XmlNode template, int indent, int insertPosition,
			ArrayList path, ObjSeqHashMap reuseMap)
		{
			CheckDisposed();
			int insertPos = insertPosition;
			foreach (XmlNode node in template.ChildNodes)
			{
				if (node.Name == "ChangeRecordHandler")
					continue;	// Handle only at the top level (at least for now).
				insertPos = ApplyLayout(obj, parentSlice, node, indent, insertPos, path, reuseMap);
			}
			return insertPos;
		}

		// Must be overridden if nulls will be inserted into items; when real item is needed,
		// this is called to create it.
		public virtual Slice MakeEditorAt(int i)
		{
			CheckDisposed();
			return null; // todo JohnT: return false;
		}

		// Get or create the real slice at index i.
		public Slice FieldAt(int i)
		{
			CheckDisposed();
			Slice slice = FieldOrDummyAt(i);
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
						m_layoutState = oldState;
				}
			}
			return slice;
		}
		/// <summary>
		/// This version expands nulls but not dummy slices. Dummy slices
		/// should know their indent.
		/// </summary>
		/// <param name="i"></param>
		/// <returns></returns>
		public Slice FieldOrDummyAt(int i)
		{
			CheckDisposed();

			var slice = Slices[i] as Slice;
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
			CheckDisposed();
			if (m_layoutState == LayoutStates.klsChecking)
			{
				SuspendLayout();
				m_layoutState = LayoutStates.klsLayoutSuspended;
			}
		}

		public bool PrepareToGoAway()
		{
			CheckDisposed();
			if (m_mediator != null && m_mediator.PropertyTable != null)
			{
				string sCurrentPartName = null;
				Guid guidCurrentObj = Guid.Empty;
				if (m_currentSlice != null)
				{
					if (m_currentSlice.ConfigurationNode != null &&
						m_currentSlice.ConfigurationNode.ParentNode != null)
					{
						sCurrentPartName = XmlUtils.GetAttributeValue(m_currentSlice.ConfigurationNode.ParentNode,
							"id", String.Empty);
					}
					if (m_currentSlice.Object != null)
						guidCurrentObj = m_currentSlice.Object.Guid;
				}
				SetCurrentSlicePropertyNames();
				m_mediator.PropertyTable.SetProperty(m_sPartNameProperty, sCurrentPartName, false, PropertyTable.SettingsGroup.LocalSettings);
				m_mediator.PropertyTable.SetProperty(m_sObjGuidProperty, guidCurrentObj, false, PropertyTable.SettingsGroup.LocalSettings);
				m_mediator.PropertyTable.SetPropertyPersistence(m_sPartNameProperty, true, PropertyTable.SettingsGroup.LocalSettings);
				m_mediator.PropertyTable.SetPropertyPersistence(m_sObjGuidProperty, true, PropertyTable.SettingsGroup.LocalSettings);
			}
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
				return;
			bool fNeedInternalLayout = true; // call HandleLayout1 at least once

			var smallestSize = new Size();
			// if we don't converge in three iterations, we probably never will. It is possible to get
			// in to an infinite loop, when scrollbars appear and disappear changing the width on every
			// iteration
			for (int i = 0; i < 3; i++)
			//for ( ; ; )
			{
				int clientWidth = ClientRectangle.Width;
				// Somehow this sometimes changes the scroll position.
				// This might be reasonable if it changed the range, but it does it other times.
				// I can't figure out why, so just force it back if it does. Grrrr!
				Point aspOld = AutoScrollPosition;
				base.OnLayout(levent);
				if (AutoScrollPosition != aspOld)
					AutoScrollPosition = new Point (-aspOld.X, -aspOld.Y);

				if (smallestSize.IsEmpty || ClientSize.Width < smallestSize.Width)
					smallestSize = ClientSize;
				// If that changed the width of our client rectangle we definitely need to
				// call HandleLayout1 again.
				fNeedInternalLayout |= (clientWidth != ClientRectangle.Width);
				if (!fNeedInternalLayout)
					return;

				fNeedInternalLayout = false; // don't need to do again unless client rect width changes.
				Rectangle clipRect = ClientRectangle;
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
						m_layoutState = LayoutStates.klsNormal;
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
				// apppear and disappear as required by more or less slices...
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
		/// <param name="fFull">if set to <c>true</c> [f full].</param>
		/// <param name="clipRect">The clip rect.</param>
		/// <returns></returns>
		protected internal int HandleLayout1(bool fFull, Rectangle clipRect)
		{
			if (m_fDisposing)
				return clipRect.Bottom; // don't want to lay out while clearing slices in dispose!

			int minHeight = GetMinFieldHeight();
			int desiredWidth = ClientRectangle.Width;

#if __MonoCS__ // FWNX-370: work around https://bugzilla.novell.com/show_bug.cgi?id=609596
			if (VerticalScroll.Visible)
				desiredWidth -= SystemInformation.VerticalScrollBarWidth;
#endif
			Point oldPos = AutoScrollPosition;
			var desiredScrollPosition = new Point(-oldPos.X, -oldPos.Y);

			int yTop = AutoScrollPosition.Y;
			for (int i = 0; i < Slices.Count; i++)
			{
				// Don't care about items below bottom of clip, if one is specified.
				if ((!fFull) && yTop >= clipRect.Bottom)
				{
					return yTop - AutoScrollPosition.Y; // not very meaningful in this case, but a result is required.
				}
				var tci = Slices[i] as Slice;
				// Best guess of its height, before we ensure it's real.
				int defHeight = tci == null ? minHeight : tci.Height;
				bool fSliceIsVisible = !fFull && yTop + defHeight > clipRect.Top && yTop <= clipRect.Bottom;

				//Debug.WriteLine(String.Format("DataTree.HandleLayout1({3},{4}): fSliceIsVisible = {5}, i = {0}, defHeight = {1}, yTop = {2}, desiredWidth = {7}, tci.Config = {6}",
				//	i, defHeight, yTop, fFull, clipRect.ToString(), fSliceIsVisible, tci.ConfigurationNode.OuterXml, desiredWidth));

				if (fSliceIsVisible)
				{
					// We cannot allow slice to be unreal; it's visible, and we're checking
					// for real slices where they're visible
					tci = FieldAt(i); // ensures it becomes real if needed.
					var dummy = tci.Handle; // also force it to get a handle
					if (tci.Control != null)
						dummy = tci.Control.Handle; // and its control must too.
					if (yTop < 0)
					{
						// It starts above the top of the window. We need to adjust the scroll position
						// by the difference between the expected and actual heights.
						// This can have side effects, don't do unless needed.
						// The slice will now handle the conditioanl execution.
						//if (tci.Width != desiredWidth)
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
						yTop += HeavyweightRuleThickness + HeavyweightRuleAboveMargin;
					if (tci.Top != yTop)
						tci.Top = yTop;
					// This can have side effects, don't do unless needed.
					// The slice will now handle the conditional execution.
					//if (tci.Width != desiredWidth)
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
			// change in the visble position of previously visible slices.
			// The scroll position may also have changed as a result of the blankety blank
			// blank undocumented behavior of the UserControl class trying to make what it
			// thinks is the interesting child control visible.
			// In case it changed, try to change it back!
			// (This might not always succeed, if the scroll range changed so as to make the old position invalid.
			if (-AutoScrollPosition.Y != desiredScrollPosition.Y)
				AutoScrollPosition = desiredScrollPosition;
			return yTop - AutoScrollPosition.Y;
		}

		private void MakeSliceRealAt(int i)
		{
			// We cannot allow slice to be unreal; it's visible, and we're checking
			// for real slices where they're visible
			Point oldPos = AutoScrollPosition;
			var tci = Slices[i];
			int oldHeight = tci == null ? GetMinFieldHeight() : tci.Height;
			tci = FieldAt(i); // ensures it becomes real if needed.
			int desiredWidth = ClientRectangle.Width;
			if (tci.Width != desiredWidth)
				tci.SetWidthForDataTreeLayout(desiredWidth); // can have side effects, don't do unless needed.
			// In the course of becoming real it may have changed height (more strictly, its
			// real height may be different from the previous estimated height).
			// If it was previously above the top of the window, this can produce an unwanted
			// change in the visble position of previously visible slices.
			// The scroll position may also have changed as a result of the blankety blank
			// blank undocumented behavior of the UserControl class trying to make what it
			// thinks is the interesting child control visible.

			// desiredScrollPosition.y is typically positive, the number of pixels hidden at the top
			// of the view before we started.
			var desiredScrollPosition = new Point(-oldPos.X, -oldPos.Y);
			// topAbs is the position of the slice relative to the top of the whole view contents now.
			int topAbs = tci.Top - AutoScrollPosition.Y;
			MakeSliceVisible(tci); // also required for it to be a real tab stop.

			if (topAbs < desiredScrollPosition.Y)
			{
				// It was above the top of the window. We need to adjust the scroll position
				// by the difference between the expected and actual heights.
				desiredScrollPosition.Y -= (oldHeight - tci.Height);
			}
			if (-AutoScrollPosition.Y != desiredScrollPosition.Y)
				AutoScrollPosition = desiredScrollPosition;
		}

		/// <summary>
		/// Make a slice visible, either because it needs to be drawn, or because it needs to be
		/// focused.
		/// </summary>
		/// <param name="tci"></param>
		internal static void MakeSliceVisible(Slice tci)
		{
			// It intersects the screen so it needs to be visible.
			if (!tci.Visible)
			{
				int index = tci.IndexInContainer;
				// All previous slices must be "visible".  Otherwise, the index of the current
				// slice gets changed when it becomes visible due to what is presumably a bug
				// in the dotnet framework.
				for (int i = 0; i < index; ++i)
				{
					Control ctrl = tci.ContainingDataTree.Slices[i];
					if (ctrl != null && !ctrl.Visible)
						ctrl.Visible = true;
				}
				tci.Visible = true;
				Debug.Assert(tci.IndexInContainer == index,
					String.Format("MakeSliceVisible: slice '{0}' at index({2}) should not have changed to index ({1})." +
					" This can occur when making slices visible in an order different than their order in DataTree.Slices. See LT-7307.",
					(tci.ConfigurationNode != null && tci.ConfigurationNode.OuterXml != null ? tci.ConfigurationNode.OuterXml : "(DummySlice?)"),
				tci.IndexInContainer, index));
				// This was moved out of the Control setter because it prematurely creates
				// root boxes (because it creates a window handle). The embedded control shouldn't
				// need an accessibility name before it is visible!
				if (!string.IsNullOrEmpty(tci.Label) && tci.Control != null && tci.Control.AccessibilityObject != null)
					tci.Control.AccessibilityObject.Name = tci.Label;// + "ZZZ_Slice";
			}
			tci.ShowSubControls();
		}

		public int GetMinFieldHeight()
		{
			CheckDisposed();
			return 18; // Enhance Johnt: base on default font height
		}
		//
		//	Return the next field index that is at the specified indent level, or zero if there are no
		//	fields following this one that are at the specified level in the tree (at least not before one
		//  at a higher level). This is normally
		//	used to find the beginning of the next subrecord when we have a sequence of subrecords,
		//	and possibly sub-subrecords, with some being expanded and others not.
		//	@param nInd The indent level we want.
		//	@param idfe An index to the current field. We start looking at the next field.
		//	@return The index of the next field or 0 if none.
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="FieldOrDummyAt() returns a reference")]
		public int NextFieldAtIndent(int nInd, int iStart)
		{
			CheckDisposed();
			int cItem = Slices.Count;

			// Start at the next editor and work down, skipping more nested editors.
			for (int i =iStart + 1; i < cItem; ++i)
			{
				int nIndCur = FieldOrDummyAt(i).Indent;
				if (nIndCur == nInd) // We found another item at this level, so return it.
					return i;
				if (nIndCur < nInd) // We came out to a higher level, so return zero.
					return 0;
			}
			return 0; // Reached the end without finding one at the specified level.
		}
		//
		//	Return the previous field index that is at the specified indent level, or zero if there are no
		//	fields preceding this one that are at the specified level in the tree (at least not before one
		//  at a higher level). This is normally used to find a parent record; some of the intermediate
		//	records may not be expanded.
		//	@param nInd The indent level we want.
		//	@param idfe An index to the current field. We start looking at the previous field.
		//	@return The index of the desired field or 0 if none.
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="FieldOrDummyAt() returns a reference")]
		public int PrevFieldAtIndent(int nInd, int iStart)
		{
			CheckDisposed();
			// Start at the next editor and work down, skipping more nested editors.
			for (int i =iStart - 1; i >= 0; --i)
			{
				int nIndCur = FieldOrDummyAt(i).Indent;
				if (nIndCur == nInd) // We found another item at this level, so return it.
					return i;
				if (nIndCur < nInd) // We came out to a higher level, so return zero.
					return 0;
			}
			return 0; // Reached the start without finding one at the specified level.
		}

		/// <summary>
		/// Answer the height that the slice at index ind is considered to have.
		/// If it is null return the default size.
		/// </summary>
		/// <param name="iSlice">The index.</param>
		/// <returns></returns>
		int HeightOfSliceOrNullAt(int iSlice)
		{
			var tc = Slices[iSlice] as Slice;
			int dypFieldHeight = GetMinFieldHeight();
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
			CheckDisposed();
			int ypTopOfNextField = 0;
			for (int iSlice = 0; iSlice < Slices.Count; iSlice++)
			{

				int dypFieldHeight = HeightOfSliceOrNullAt(iSlice);
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
				Rectangle requiredReal = ClientRectangle; // all slices in this must be real
				HandleLayout1(false, requiredReal);
				bool fNeedResume = (m_layoutState == LayoutStates.klsLayoutSuspended);
				m_layoutState = LayoutStates.klsNormal;
				if (fNeedResume)
				{
					Point oldPos = AutoScrollPosition;
					ResumeLayout(); // will cause another paint (and may cause unwanted scroll of parent!)
					Invalidate(false); // Well, apparently doesn't always do so...we were sometimes not getting the lines. Make sure.
					PerformLayout();
					if (AutoScrollPosition != oldPos)
						AutoScrollPosition = new Point(-oldPos.X, -oldPos.Y);
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

		new public Control ActiveControl
		{
			set
			{
				CheckDisposed();

				if (base.ActiveControl == value)
					return;

				base.ActiveControl = value;
				foreach (Slice slice in Slices)
				{
					if ((slice.Control == value || slice == value) && m_currentSlice != slice)
						CurrentSlice = slice;
				}
			}
		}

		#region automated tree navigation

		/// <summary>
		/// Moves the focus to the first visible slice in the tree
		/// </summary>
		public void GotoFirstSlice()
		{
			CheckDisposed();
			GotoNextSliceAfterIndex(-1);
		}

		public Slice LastSlice
		{
			get
			{
				CheckDisposed();
				if (Slices.Count == 0)
					return null;
				return Slices.Last();
			}
		}

		/// <summary>
		/// Moves the focus to the next visible slice in the tree
		/// </summary>
		public void GotoNextSlice()
		{
			CheckDisposed();

			if (m_currentSlice != null)
				GotoNextSliceAfterIndex(Slices.IndexOf(m_currentSlice));
		}

		internal bool GotoNextSliceAfterIndex(int index)
		{
			CheckDisposed();
			++index;
			while (index >= 0 && index < Slices.Count)
			{
				Slice current = FieldAt(index);
				MakeSliceVisible(current);
				if (current.TakeFocus(false))
				{
					if (m_currentSlice != current)
						CurrentSlice = current; // We are going to it, so make it current.
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
			CheckDisposed();
			--index;
			while (index >= 0 && index < Slices.Count)
			{
				Slice current = FieldAt(index);
				MakeSliceVisible(current);
				if (current.TakeFocus(false))
				{
					if (m_currentSlice != current)
						CurrentSlice = current; // We are going to it, so make it current.
					return true;
				}
				--index;
			}
			return false;
		}

		#endregion automated tree navigation

		#region IxCoreColleague implementation

		public void Init(Mediator mediator, XmlNode configurationParameters)
		{
			CheckDisposed();
			m_mediator = mediator;
			m_sliceSplitPositionBase = XmlUtils.GetOptionalIntegerValue(configurationParameters,
				"defaultLabelWidth", m_sliceSplitPositionBase);
			// This needs to happen AFTER we set the configuration sliceSplitPositionBase, otherwise,
			// it will override the persisted value.
			if (PersistenceProvder != null)
				RestorePreferences();
		}

		public IxCoreColleague[] GetMessageTargets()
		{
			CheckDisposed();

			if (Visible)
			{
				if (m_currentSlice != null)
					return new IxCoreColleague[] { m_currentSlice, this };
				return new IxCoreColleague[] {this};
			}

			// If we're not visible, we don't want to be a message target.
			// It is remotely possible that the current slice still does, though.
			return m_currentSlice != null ? new IxCoreColleague[] {m_currentSlice} : new IxCoreColleague[0];
		}

		/// <summary>
		/// Should not be called if disposed (or in the process of disposing).
		/// </summary>
		public bool ShouldNotCall
		{
			get { return IsDisposed || m_fDisposing; }
		}

		/// <summary>
		/// Mediator message handling Priority
		/// </summary>
		public int Priority
		{
			get { return (int)ColleaguePriority.Medium; }
		}

		#endregion IxCoreColleague implementation

		#region IxCoreColleague message handlers

		/// <summary>
		/// This property may be turned on and off any time a DataTree is an active colleague.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayShowHiddenFields(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();
			bool fAllow = Mediator.PropertyTable.GetBoolProperty("AllowShowNormalFields", true);
			display.Enabled = display.Visible = fAllow;

			if (display.Enabled)
			{
				// The boolProperty of this menu item isn't the real one, so we control the checked status
				// from here.  See the OnPropertyChanged method for how changes are handled.
				string toolName = m_mediator.PropertyTable.GetStringProperty("currentContentControl", null);
				display.Checked = m_mediator.PropertyTable.GetBoolProperty("ShowHiddenFields-" + toolName, false, PropertyTable.SettingsGroup.LocalSettings);
			}

			return true; //we've handled this
		}

		/// <summary>
		/// Enable/Disable menu items for jumping to the Lexicon Edit tool and applying a column filter on the Anthropology Category
		/// the user has right clicked on.
		/// </summary>
		public virtual bool OnDisplayJumpToLexiconEditFilterAnthroItems(object commandObject, ref UIItemDisplayProperties display)
		{
			return DisplayJumpToToolAndFilterAnthroItem(display, commandObject, "CmdJumpToLexiconEditWithFilter");
		}

		/// <summary>
		/// Enable/Disable menu items for jumping to the Notebook Edit tool and applying a column filter on the Anthropology Category
		/// the user has right clicked on.
		/// </summary>
		public virtual bool OnDisplayJumpToNotebookEditFilterAnthroItems(object commandObject, ref UIItemDisplayProperties display)
		{
			return DisplayJumpToToolAndFilterAnthroItem(display, commandObject, "CmdJumpToNotebookEditWithFilter");
		}

		private bool DisplayJumpToToolAndFilterAnthroItem(UIItemDisplayProperties display, object commandObject, string cmd)
		{
			CheckDisposed();

			if (display.Group != null && display.Group.IsContextMenu &&
				!String.IsNullOrEmpty(display.Group.Id) &&
				!display.Group.Id.StartsWith("mnuReferenceChoices"))
			{
				return false;
			}

			var fieldName = XmlUtils.GetOptionalAttributeValue(CurrentSlice.ConfigurationNode, "field");
			if (String.IsNullOrEmpty(fieldName) || !fieldName.Equals("AnthroCodes"))
			{
				display.Enabled = display.Visible = false;
				return true;
			}

			var xmlNode = (commandObject as XCore.Command).ConfigurationNode;
			var command = XmlUtils.GetOptionalAttributeValue(xmlNode, "id");
			if (String.IsNullOrEmpty(command))
				return false;
			if (command.Equals(cmd))
				display.Enabled = display.Visible = true;
			else
				display.Enabled = display.Visible = false;
			return true;
		}

		/// <summary>
		/// Enable menu items for jumping to the concordance (or lexiconEdit) tool.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayJumpToTool(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();
			string tool;
			if (display.Group != null && display.Group.IsContextMenu &&
				!String.IsNullOrEmpty(display.Group.Id) &&
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
		public virtual bool OnJumpToTool(object commandObject)
		{
			CheckDisposed();
			string tool;
			Guid guid = GetGuidForJumpToTool((Command) commandObject, false, out tool);
			if (guid != Guid.Empty)
			{
				m_mediator.PostMessage("FollowLink", new FwLinkArgs(tool, guid));
				((Command)commandObject).TargetId = Guid.Empty;	// clear the target for future use.
				return true;
			}

			return false;
		}

		/// <summary>
		/// Handle jumping to the lexiconEdit tool and filtering on the Anthropology Category the user has
		/// right clicked on.
		/// </summary>
		public virtual bool OnJumpToLexiconEditFilterAnthroItems(object commandObject)
		{
			OnJumpToToolAndFilterAnthroItem("FilterAnthroItems", "lexiconEdit");
			return true;
		}

		/// <summary>
		/// Handle jumping to the NotebookEdit tool and filtering on the Anthropology Category the user has
		/// right clicked on.
		/// </summary>
		public virtual bool OnJumpToNotebookEditFilterAnthroItems(object commandObject)
		{
			OnJumpToToolAndFilterAnthroItem("FilterAnthroItems", "notebookEdit");
			return true;
		}

		private void OnJumpToToolAndFilterAnthroItem(string linkSetupInfo, string toolToJumpTo)
		{
			var rootBx
				= ((CurrentSlice.Control as VectorReferenceLauncher).MainControl as VectorReferenceView).RootBox;
			var selection = rootBx.Selection;

			// Enhance GJM: I don't like the way this matches the Abbreviation-Name string. We should be getting the
			// hvos of the objects (at least partially) selected from views. (See LT-12240)
			ITsString TsStr;
			selection.GetSelectionString(out TsStr, "");
			var sMatchText = TsStr.Text;

			var repoAnthroCats = m_cache.ServiceLocator.GetInstance<ICmAnthroItemRepository>();
			var hvoList = (from item in repoAnthroCats.AllInstances()
						   where sMatchText.Contains(item.AbbrAndName)
						   select item.Hvo).ToList();
			if (hvoList.Count == 0)
			{
				// For now just don't do anything if we didn't get an exact match.
				// This can happen, for instance, if we try to select more than one Anthro category to filter on.
				return;
			}
			var shvos = ConvertHvoListToString(hvoList);

			FwLinkArgs link = new FwAppArgs(FwUtils.FwUtils.ksFlexAppName, Cache.ProjectId.Handle,
											Cache.ProjectId.ServerName, toolToJumpTo, Guid.Empty);
			List<Property> additionalProps = link.PropertyTableEntries;
			additionalProps.Add(new Property("SuspendLoadListUntilOnChangeFilter", link.ToolName));
			additionalProps.Add(new Property("LinkSetupInfo", linkSetupInfo));
			additionalProps.Add(new Property("HvoOfAnthroItem", shvos));
			m_mediator.PostMessage("FollowLink", link);
		}

		/// <summary>
		/// Converts a List of integers into a comma-delimited string of numbers.
		/// </summary>
		/// <param name="hvoList"></param>
		/// <returns></returns>
		private string ConvertHvoListToString(List<int> hvoList)
		{
			return hvoList.ToString(",");
		}

		/// <summary>
		/// Common logic shared between OnDisplayJumpToTool and OnJumpToTool.
		/// forEnableOnly is true when called from OnDisplayJumpToTool.
		/// </summary>
		private Guid GetGuidForJumpToTool(Command cmd, bool forEnableOnly, out string tool)
		{
			tool = XmlUtils.GetManditoryAttributeValue(cmd.Parameters[0], "tool");
			string className = XmlUtils.GetManditoryAttributeValue(cmd.Parameters[0], "className");
			if (CurrentSlice != null && CurrentSlice.Object != null)
			{
				var owner = CurrentSlice.Object.Owner;
				if (tool == "concordance")
				{
					int flidSlice = 0;
					if (!CurrentSlice.IsHeaderNode)
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

								if (CurrentSlice.Object.ClassID == LexEntryRefTags.kClassId)
									return cmd.TargetId;

								if (CurrentSlice.Object.ClassID == LexEntryTags.kClassId)
									return CurrentSlice.Object.Guid;

								var lexEntry = CurrentSlice.Object.OwnerOfClass<ILexEntry>();
								return lexEntry == null ? cmd.TargetId : lexEntry.Guid;
							}
							break;
						case "LexSense":
							if (CurrentSlice.Object.ClassID == LexSenseTags.kClassId)
							{
								if (((ILexSense)CurrentSlice.Object).Entry == m_root)
									return CurrentSlice.Object.Guid;
							}
							break;
						case "MoForm":
							if (m_cache.ClassIsOrInheritsFrom(CurrentSlice.Object.ClassID, MoFormTags.kClassId))
							{
								if (flidSlice == MoFormTags.kflidForm)
									return CurrentSlice.Object.Guid;
							}
							break;
					}
				}
				else if (tool == "lexiconEdit")
				{
					if (owner != null && owner != m_root && owner.ClassID == LexEntryTags.kClassId)
					{
						return owner.Guid;
					}
				}
				else if (tool == "notebookEdit")
				{
					if (owner != null &&
						owner.ClassID == RnGenericRecTags.kClassId)
						return owner.Guid;
					if (CurrentSlice.Object is IText)
					{
						// Text is not already owned by a notebook record. So there's nothing yet to jump to.
						// If the user is really doing the jump we need to make it now.
						// Otherwise we just need to return something non-null to indicate the jump
						// is possible (though this is not currently used).
						if (forEnableOnly)
							return CurrentSlice.Object.Guid;
						((IText) CurrentSlice.Object).MoveToNotebook(true);
						return CurrentSlice.Object.Owner.Guid;
					}
					// Try TargetId by default
				}
				else if (tool == "interlinearEdit")
				{
					if (CurrentSlice.Object.ClassID == TextTags.kClassId)
					{
						return CurrentSlice.Object.Guid;
					}
				}
			}
			return cmd.TargetId;
		}
		/// <summary>
		/// Receives the broadcast message "PropertyChanged"
		/// </summary>
		public void OnPropertyChanged(string name)
		{
			CheckDisposed();

			if (name == "ShowHiddenFields")
			{
				// The only place this occurs is when the status is changed from the "View" menu.
				// We'll have to translate this to the real property based on the current tool.

				string toolName = m_mediator.PropertyTable.GetStringProperty("currentContentControl", null);
				name = "ShowHiddenFields-" + toolName;

				// Invert the status of the real property
				bool oldShowValue = m_mediator.PropertyTable.GetBoolProperty(name, false, PropertyTable.SettingsGroup.LocalSettings);
				m_mediator.PropertyTable.SetProperty(name, !oldShowValue, true, PropertyTable.SettingsGroup.LocalSettings); // update the pane bar check box.
				HandleShowHiddenFields(!oldShowValue);
			}
			else if (name.StartsWith("ShowHiddenFields-"))
			{
				bool fShowAllFields = m_mediator.PropertyTable.GetBoolProperty(name, false, PropertyTable.SettingsGroup.LocalSettings);
				HandleShowHiddenFields(fShowAllFields);
			}
			else if (name == "currentContentControlObject")
			{
				m_fCurrentContentControlObjectTriggered = true;
			}
		}

		/// <summary>
		/// Called by reflection when a new object is inserted into the list. A change of current
		/// object should not ALWAYS make the data tree take focus, since that can be annoying when
		/// editing in the browse view (cf LT-8211). But we do want it for a new top-level list object
		/// (LT-8564). Also useful when we refresh the list after a major change to make sure something gets focused.
		/// </summary>
		/// <param name="arg"></param>
		public void OnFocusFirstPossibleSlice(object arg)
		{
			m_mediator.IdleQueue.Add(IdleQueuePriority.Medium, DoPostponedFocusSlice);
		}

		bool DoPostponedFocusSlice(object parameter)
		{
			// If the user switches tools quickly after inserting an object, the new view may
			// already be created before this gets called to set the focus in the old view.
			// Therefore we don't want to crash, we just want to do nothing.  See LT-8698.
			if (IsDisposed)
				return true;
			if (CurrentSlice == null)
				FocusFirstPossibleSlice();
			return true;
		}

		private void HandleShowHiddenFields(bool newShowValue)
		{
			if (newShowValue != m_fShowAllFields)
			{

				MonoIgnoreUpdates();

				try
				{
					var closeSlices = CurrentSlice == null ? null : CurrentSlice.GetCloseSlices();
					m_fShowAllFields = newShowValue;
					RefreshList(false);
					if (closeSlices != null)
						SelectFirstPossibleSlice(closeSlices);
					ScrollCurrentAndIfPossibleSectionIntoView();
				}
				finally
				{
					MonoResumeUpdates();
				}
			}
		}

		/// <summary>
		/// For sure make the CurrentSlice if any visible.
		/// If possible also make the prececing summary slice visible.
		/// Then make as many as possible of the slices which are children of that summary visible.
		/// </summary>
		void ScrollCurrentAndIfPossibleSectionIntoView()
		{
			if (CurrentSlice == null)
				return; // can't do anything.
			// Make sure all the slices up to one screen above and below are real and valid heights.
			// This is only called in response to a user action, so m_layoutState should be normal.
			// We set this state to make quite sure that if we somehow get an OnPaint() or OnLayout call
			// that is effectively re-entrant, we don't re-enter HandleLayout1, which can really mess things up.
			Debug.Assert(m_layoutState == LayoutStates.klsNormal);
			m_layoutState = LayoutStates.klsDoingLayout;
			try
			{
				HandleLayout1(false, new Rectangle(0, Math.Max(0, CurrentSlice.Top - ClientRectangle.Height),
					ClientRectangle.Width, ClientRectangle.Height * 2));
			}
			finally
			{
				m_layoutState = LayoutStates.klsNormal;
			}
			ScrollControlIntoView(CurrentSlice);
			int previousSummaryIndex = CurrentSlice.IndexInContainer;
			while (!(Slices[previousSummaryIndex] is SummarySlice))
			{
				previousSummaryIndex--;
				if (previousSummaryIndex < 0)
					return;
			}
			var previousSummary = Slices[previousSummaryIndex];
			if (previousSummary.Top < 0 && CurrentSlice.Bottom - previousSummary.Top < ClientRectangle.Height - 20)
				ScrollControlIntoView(previousSummary);
			var lastChildIndex = CurrentSlice.IndexInContainer;
			while (lastChildIndex < Slices.Count && Slice.StartsWith(((Slice)Slices[lastChildIndex]).Key, previousSummary.Key)
				&& Slices[lastChildIndex].Bottom - previousSummary.Top < ClientRectangle.Height - 20)
				lastChildIndex++;
			lastChildIndex--;
			if (lastChildIndex > CurrentSlice.IndexInContainer)
				ScrollControlIntoView(Slices[lastChildIndex]);
		}

		/// <summary>
		/// Find the first slice in the list which is (still) one of your current, valid slices
		/// and which is able to take focus, and give it the focus.
		/// </summary>
		/// <param name="closeSlices"></param>
		internal void SelectFirstPossibleSlice(List<Slice> closeSlices)
		{
			foreach (var slice in closeSlices)
			{
				if (!slice.IsDisposed && slice.ContainingDataTree == this && slice.TakeFocus(false))
					break;
			}
		}

		/// <summary>
		/// Invoked by a slice when the user does something to bring up a context menu
		/// </summary>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="See TODO comment.")]
		public void OnShowContextMenu(object sender, TreeNodeEventArgs e)
		{
			CheckDisposed();
			//just pass this onto, for example, the XWorks View that owns us,
			//assuming that it has subscribed to this event on this object.
			//If it has not, then this will still point to the "auto menu handler"
			Debug.Assert(ShowContextMenuEvent != null, "this should always be set to something");
			CurrentSlice = e.Slice;
			var args = new SliceMenuRequestArgs(e.Slice, false);
			// TODO: ShowContextMenuEvent returns a ContextMenu that we should dispose. However,
			// we can't do that right here (because that destroys the menu before being shown).
			// Ideally we would store the context menu in a member variable and dispose this later
			// on. However, it is unlikely that not disposing this context menu will cause any
			// problems, so we leave it as is for now.
			ShowContextMenuEvent(sender, args);
			//			ContextMenu menu = ShowContextMenuEvent(sender, args);
			//			menu.Show(e.Context, e.Location);
		}

		/// <summary>
		/// Process the message to allow setting/focusing CurrentSlice.
		/// </summary>
		/// <param name="parameter">The parameter.</param>
		/// <returns></returns>
		public bool OnReadyToSetCurrentSlice(object parameter)
		{
			if (IsDisposed)
				return true;

			// we should now be ready to put our focus in a slice.
			m_fSuspendSettingCurrentSlice = false;
			try
			{
				SetDefaultCurrentSlice((bool) parameter);
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
			CheckDisposed();
			DoNotRefresh = (bool)sentValue;
		}

		/// <summary>
		/// subclasses override for setting a default current slice.
		/// </summary>
		/// <returns></returns>
		protected virtual void SetDefaultCurrentSlice(bool suppressFocusChange)
		{
			Slice sliceToSetAsCurrent = m_currentSliceNew;
			m_currentSliceNew = null;
			if (sliceToSetAsCurrent != null && sliceToSetAsCurrent.IsDisposed)
				sliceToSetAsCurrent = null;	// someone's creating slices faster than we can display!
			// try to see if any of our current slices have focus. if so, use that one.
			if (sliceToSetAsCurrent == null)
			{
			Control focusedControl = XWindow.FocusedControl();
			if (ContainsFocus)
			{
				// see if we can find the parent slice for focusedControl
				Control currentControl = focusedControl;
				while (currentControl != null && currentControl != this)
				{
					if (currentControl is Slice)
					{
						// found the slice to
						sliceToSetAsCurrent = currentControl as Slice;
							if (sliceToSetAsCurrent.IsDisposed)
								sliceToSetAsCurrent = null;		// shouldn't happen, but...
							else
						break;
					}
					currentControl = currentControl.Parent;
				}
			}
			}
			// set current slice.
			if (sliceToSetAsCurrent != null)
			{
				CurrentSlice = sliceToSetAsCurrent;
				if (!suppressFocusChange && !m_currentSlice.Focused && m_fCurrentContentControlObjectTriggered)	// probably coming from m_currentSliceNew
				{
					// For string type slices, place cursor at end of (top) line.  This works
					// more reliably than putting it at the beginning for some reason, and makes
					// more sense in some circumstances (especially in the conversion from a ghost
					// slice to a string type slice).
					if (m_currentSlice is MultiStringSlice)
					{
						var mss = (MultiStringSlice) m_currentSlice;
						mss.SelectAt(mss.WritingSystemsSelectedForDisplay.First().Handle, 99999);
					}
					else if (m_currentSlice is StringSlice)
					{
						((StringSlice) m_currentSlice).SelectAt(99999);
					}
					m_currentSlice.TakeFocus(false);
				}
			}
			// otherwise, try to select the first slice, if it won't conflict with
			// an existing cursor (cf. LT-8211), like when we're first starting up/switching tools
			// as indicated by m_fCurrentContentControlObjectTriggered.
			if (!suppressFocusChange && CurrentSlice == null && m_fCurrentContentControlObjectTriggered)
				FocusFirstPossibleSlice();
		}

		/// <summary>
		/// Focus the first slice that can take focus.
		/// </summary>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="FieldOrDummyAt() and FieldAt() return a reference.")]
		protected bool FocusFirstPossibleSlice()
		{
			int cslice = Slices.Count;
			// If we have a descendant that isn't the root, try to focus one of its slices.
			// Otherwise, focusing a slice that doesn't belong to it will switch the browse view
			// to a different record. See FWR-2006.
			if (m_descendant != null && m_descendant != m_root)
			{
				var owners = new HashSet<ICmObject>();
				for (var obj = m_descendant; obj != null; obj = obj.Owner)
					owners.Add(obj);
				for (int islice = 0; islice < cslice; ++islice)
				{
					var slice = FieldOrDummyAt(islice);
					if (slice is DummyObjectSlice && owners.Contains(slice.Object))
					{
						// This is what we want! Expand it!
						slice = FieldAt(islice); // makes a real slice (and may create children, altering the total number).
						cslice = Slices.Count;
					}
					if (m_descendant != DescendantForSlice(slice))
						continue;
					if (slice.TakeFocus(false))
						return true;
				}
			}
			// If that didn't work or we don't have a distinct descendant, just focus the first thing we can.
			for (int islice = 0; islice < cslice; ++islice)
			{
				var slice = Slices[islice];
				if (slice.TakeFocus(false))
					return true;
			}
			return false;
		}

		#endregion IxCoreColleague message handlers

		/// <summary>
		/// Influence the display of a particular command by giving an opinion on whether we
		/// are prepared to handle the corresponding "InsertItemViaBackrefVector" message.
		/// </summary>
		public bool OnDisplayInsertItemViaBackrefVector(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			// We may be in transition: if so, disable without crashing.  See LT-9698.
			if (m_cache == null || m_root == null)
				return display.Enabled = false;
			var command = (Command)commandObject;
			string className = XmlUtils.GetManditoryAttributeValue(command.Parameters[0], "className");
			if (className != m_root.ClassName)
				return display.Enabled = false;
			string restrictToTool = XmlUtils.GetOptionalAttributeValue(command.Parameters[0], "restrictToTool");
			if (restrictToTool != null && restrictToTool != m_mediator.PropertyTable.GetStringProperty("currentContentControl", String.Empty))
				return display.Enabled = false;
			return display.Enabled = true;
		}

		/// <summary>
		/// This is triggered by any command whose message attribute is "InsertItemViaBackrefVector"
		/// </summary>
		/// <returns>true if successful (the class is known)</returns>
		public bool OnInsertItemViaBackrefVector(object argument)
		{
			CheckDisposed();

			var command = (Command)argument;
			string className = XmlUtils.GetManditoryAttributeValue(command.Parameters[0], "className");
			if (className != m_root.ClassName)
				return false;
			string restrictToTool = XmlUtils.GetOptionalAttributeValue(command.Parameters[0], "restrictToTool");
			if (restrictToTool != null && restrictToTool != m_mediator.PropertyTable.GetStringProperty("currentContentControl", String.Empty))
				return false;
			string fieldName = XmlUtils.GetOptionalAttributeValue(command.Parameters[0], "fieldName");
			if (String.IsNullOrEmpty(fieldName))
				return false;
			int flid = m_mdc.GetFieldId(className, fieldName, true);
			int insertPos = Slice.InsertObjectIntoVirtualBackref(m_cache, m_mediator,
				m_root.Hvo, m_root.ClassID, flid);
			return insertPos >= 0;
		}

		/// <summary>
		/// See if it makes sense to provide the "Demote..." command.
		/// </summary>
		public bool OnDisplayDemoteItemInVector(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			var command = (Command)commandObject;
			string className = XmlUtils.GetManditoryAttributeValue(command.Parameters[0], "className");
			bool fIsValid = false;
			if (className == "RnGenericRec")
			{
				if (Root != null && Root is IRnGenericRec)
				{
					if (Root.Owner is IRnResearchNbk && (Root.Owner as IRnResearchNbk).RecordsOC.Count > 1)
						fIsValid = true;
				}
			}
			display.Enabled = fIsValid;
			return true;
		}
		/// <summary>
		/// Implement the "Demote..." command.
		/// </summary>
		public bool OnDemoteItemInVector(object argument)
		{
			CheckDisposed();

			if (Root == null)
				return false;
			IRnGenericRec rec = Root as IRnGenericRec;
			if (rec == null)
				return false;		// shouldn't get here
			IRnGenericRec newOwner = null;
			if (Root.Owner is IRnResearchNbk)
			{
				IRnResearchNbk notebk = Root.Owner as IRnResearchNbk;
				List<IRnGenericRec> owners = new List<IRnGenericRec>();
				foreach (var recT in notebk.RecordsOC)
				{
					if (recT != Root)
						owners.Add(recT);
				}
				if (owners.Count == 1)
				{
					newOwner = owners[0];
				}
				else
				{
					newOwner = ChooseNewOwner(owners.ToArray(),
						Resources.DetailControlsStrings.ksChooseOwnerOfDemotedRecord);
				}
			}
			else
			{
				return false;
			}
			if (newOwner == null)
				return true;
			if (newOwner == rec)
				throw new Exception("RnGenericRec cannot own itself!");

			UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(Resources.DetailControlsStrings.ksUndoDemote,
				Resources.DetailControlsStrings.ksRedoDemote, Cache.ActionHandlerAccessor, () =>
				{
					newOwner.SubRecordsOS.Insert(0, rec);
				});
			return true;
		}

		internal IRnGenericRec ChooseNewOwner(IRnGenericRec[] records, string sTitle)
		{

			var helpTopic = "khtpDataNotebook-ChooseOwnerOfDemotedRecord";
			XCore.PersistenceProvider persistProvider =
				new PersistenceProvider(m_mediator.PropertyTable);
			var labels = ObjectLabel.CreateObjectLabels(m_cache, records.Cast<ICmObject>(),
					"ShortName", m_cache.WritingSystemFactory.GetStrFromWs(m_cache.DefaultAnalWs));
			using (var dlg = new ReallySimpleListChooser(persistProvider, labels,
				String.Empty, m_mediator.HelpTopicProvider))
			{
				dlg.Text = sTitle;
				dlg.SetHelpTopic(helpTopic);
				if (dlg.ShowDialog() == DialogResult.OK)
					return dlg.SelectedObject as IRnGenericRec;
			}
			return null;
		}

		/// <summary>
		/// Try to find a slice that matches the information gleaned from another slice,
		/// probably one that has been disposed since the information was obtained.  If there's
		/// a following slice that matches except for the object id, return that slice as well.
		/// </summary>
		/// <remarks>
		/// This is used by DTMenuHandler.OnDataTreeCopy() whenever creating the copy causes the
		/// data tree to be rebuilt.  See FWR-2123 for motivation.
		/// </remarks>
		public Slice FindMatchingSlices(ICmObject obj, object[] key, Type type, out Slice newCopy)
		{
			Slice sliceFound = null;
			newCopy = null;
			foreach (Slice slice in Slices)
			{
				if (slice.GetType() != type)
					continue;
				if (EquivalentKeys(slice.Key, key, sliceFound == null))
				{
					if (slice.Object == obj)
						sliceFound = slice;
					else if (sliceFound != null && slice.Object != obj && slice.Object.ClassID == obj.ClassID)
						newCopy = slice;
					if (sliceFound != null && newCopy != null)
						break;
				}
			}
			return sliceFound;
		}

		private bool EquivalentKeys(object[] newKey, object[] oldKey, bool fCheckInts)
		{
			if (newKey.Length != oldKey.Length)
				return false;
			for (int i = 0; i < newKey.Length; ++i)
			{
				if (newKey[i] == oldKey[i])
					continue;
				if (newKey[i] is XmlNode && oldKey[i] is XmlNode)
				{
					XmlNode newNode = newKey[i] as XmlNode;
					XmlNode oldNode = oldKey[i] as XmlNode;
					if (newNode.Name != oldNode.Name)
						return false;
					if (newNode.InnerXml != oldNode.InnerXml)
						return false;
					if (newNode.OuterXml == oldNode.OuterXml)
						continue;
					foreach (XmlAttribute xa in oldNode.Attributes)
					{
						XmlAttribute xaNew = newNode.Attributes[xa.Name];
						if (xaNew == null || xaNew.Value != xa.Value)
							return false;
					}
				}
				else if (newKey[i] is int && oldKey[i] is int)
				{
					if (fCheckInts && (int)newKey[i] != (int)oldKey[i])
						return false;
				}
				else
				{
					return false;
				}
			}
			return true;
		}
	}

	class DummyObjectSlice : Slice
	{
		private XmlNode m_node; // Node with name="seq" that controls the sequence we're a dummy for
		// Path of parent slice info up to and including m_node.
		// We can't use a List<int>, as the Arraylist may hold XmlNodes and ints, at least.
		private ArrayList m_path;
		private readonly int m_flid; // sequence field we're a dummy for
		private int m_ihvoMin; // index in sequence of first object we stand for.
		private readonly string m_layoutName;
		private readonly string m_layoutChoiceField;
		private XmlNode m_caller; // Typically "partRef" node that invoked the part containing the <seq>

		/// <summary>
		/// Create a slice. Note that callers that will further modify path should pass a Clone.
		/// </summary>
		/// <param name="indent">The indent.</param>
		/// <param name="node">The node.</param>
		/// <param name="path">The path.</param>
		/// <param name="obj">The obj.</param>
		/// <param name="flid">The flid.</param>
		/// <param name="ihvoMin">The ihvo min.</param>
		/// <param name="layoutName">Name of the layout.</param>
		/// <param name="layoutChoiceField">The layout choice field.</param>
		/// <param name="caller">The caller.</param>
		public DummyObjectSlice(int indent, XmlNode node, ArrayList path, ICmObject obj, int flid, int ihvoMin,
			string layoutName, string layoutChoiceField, XmlNode caller)
		{
			m_indent = indent;
			m_node = node;
			m_path = path;
			m_obj = obj;
			m_flid = flid;
			m_ihvoMin = ihvoMin;
			m_layoutName = layoutName;
			m_layoutChoiceField = layoutChoiceField;
			m_caller = caller;
		}

		#region IDisposable override

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + "(DummyObjectSlice) 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_path != null)
					m_path.Clear();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_node = null;
			m_path = null;
			m_caller = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		public override bool IsRealSlice
		{
			get
			{
				CheckDisposed();
				return false;
			}
		}

		/// <summary>
		/// Turn this dummy slice into whatever it stands for, replacing itself in the data tree's
		/// slices (where it occupies slot index) with whatever is appropriate.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public override Slice BecomeReal(int index)
		{
			CheckDisposed();

			// We stand in for the slice at 'index', and that is to be replaced. But we might stand for earlier
			// slices too: how many indicates what we have to add to m_ihvoMin.

			// Note: I (RandyR) don't think the same one can stand in for multiple dummies now.
			// We don't use a dummy slice in more than one place.
			// Each are created individually, if more than one is needed.
			int ihvo = m_ihvoMin;
			for (int islice = index - 1; islice >= 0 && ContainingDataTree.Slices[islice] == this; islice--)
				ihvo++;
			int hvo = m_cache.DomainDataByFlid.get_VecItem(m_obj.Hvo, m_flid, ihvo);
			// In the course of becoming real, we may get disposed. That clears m_path, which
			// has various bad effects on called objects that are trying to use it, as well as
			// causing failure here when we try to remove the thing we added temporarily.
			// Work with a copy, so Dispose can't get at it.
			var path = new ArrayList(m_path);
			if (ihvo == m_ihvoMin)
			{
				// made the first element real. Increment start ihvo: the first thing we are a
				// dummy for got one greater
				m_ihvoMin++;
			}
			else if (index < ContainingDataTree.Slices.Count && ContainingDataTree.Slices[index + 1] == this)
			{
				// Any occurrences after index get replaced by a new one with suitable ihvoMin.
				// Note this must be done before we insert an unknown number of extra slices
				// by calling CreateSlicesFor.
				var dosRep = new DummyObjectSlice(m_indent, m_node, path,
					m_obj, m_flid, ihvo + 1, m_layoutName, m_layoutChoiceField, m_caller) {Cache = Cache, ParentSlice = ParentSlice};
				for (int islice = index + 1;
					islice < ContainingDataTree.Slices.Count && ContainingDataTree.Slices[islice] == this;
					islice++)
				{
					ContainingDataTree.RawSetSlice(islice, dosRep);
				}
			}

			// Save these, we may get disposed soon, can't get them from member data any more.
			DataTree containingTree = ContainingDataTree;
			Control parent = Parent;
			var parentSlice = ParentSlice;

			path.Add(hvo);
			var objItem = ContainingDataTree.Cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo);
			Point oldPos = ContainingDataTree.AutoScrollPosition;
			ContainingDataTree.CreateSlicesFor(objItem, parentSlice, m_layoutName, m_layoutChoiceField, m_indent, index + 1, path,
				new ObjSeqHashMap(), m_caller);
			// If inserting slices somehow altered the scroll position, for example as the
			// silly Panel tries to make the selected control visible, put it back!
			if (containingTree.AutoScrollPosition != oldPos)
				containingTree.AutoScrollPosition = new Point(-oldPos.X, -oldPos.Y);
			// No need to remove, we added to copy.
			//m_path.RemoveAt(m_path.Count - 1);
			return containingTree.Slices.Count > index + 1 ? containingTree.Slices[index + 1] as Slice : null;
		}

		protected override void WndProc(ref Message m)
		{
			int aspY = AutoScrollPosition.Y;
			base.WndProc (ref m);
			if (aspY != AutoScrollPosition.Y)
				Debug.WriteLine("ASP changed during processing message " + m.Msg);
		}
	}
}
