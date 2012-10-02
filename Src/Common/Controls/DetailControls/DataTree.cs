using System;
using System.Windows.Forms;
using System.ComponentModel;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Diagnostics;
using System.Runtime.InteropServices;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.Controls;
using SIL.Utils;
using SIL.FieldWorks.Common.RootSites;
using XCore;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// A DataTree displays a tree diagram alongside a collection of controls. Each control is
	/// represented as a Slice, and typically contains and actual .NET control of some
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
	public class DataTree : UserControl, IFWDisposable, IVwNotifyChange, IxCoreColleague
	{
		#region Data members

		/// <summary>
		/// Use this to do the Add/RemoveNotifications, since it can be used in the unmanged section of Dispose.
		/// (If m_sda is COM, that is.)
		/// Doing it there will be safer, since there was a risk of it not being removed
		/// in the mananged section, as when disposing was done by the Finalizer.
		/// </summary>
		private ISilDataAccess m_sda;
		/// <summary></summary>
		protected FDO.FdoCache m_cache;
		/// <summary>use SetContextMenuHandler() to subscribe to this event (if you want to provide a Context menu for this DataTree),</summary>
		protected event  SliceShowMenuRequestHandler ShowContextMenuEvent;
		//protected AutoDataTreeMenuHandler m_autoHandler;
		/// <summary></summary>
		protected int m_hvoRoot; // Typically the object we are editing.
		/// <summary></summary>
		protected IFwMetaDataCache m_mdc; // allows us to interpret class and field names and trace superclasses.
		/// <summary></summary>
		protected ICmObject m_root;
		/// <summary></summary>
		protected Slice m_currentSlice = null;
		/// <summary>used to restore current slice during RefreshList()</summary>
		private string m_currentSlicePartName = null;
		/// <summary>used to restore current slice during RefreshList()</summary>
		private int m_currentSliceObjHvo = 0;
		/// <summary>used to restore current slice during RefreshList()</summary>
		private bool m_fSetCurrentSliceNew = false;
		/// <summary>used to restore current slice during RefreshList()</summary>
		private Slice m_currentSliceNew = null;
		/// <summary>used to restore current slice during RefreshList()</summary>
		private string m_sPartNameProperty = null;
		/// <summary>used to restore current slice during RefreshList()</summary>
		private string m_sObjHvoProperty = null;
		/// <summary></summary>
		protected ImageCollection m_smallImages = null;
		/// <summary></summary>
		protected string m_rootLayoutName = "default";
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
		protected Set<KeyValuePair<int, int>> m_monitoredProps = new Set<KeyValuePair<int, int>>();
		// Number of times DeepSuspendLayout has been called without matching DeepResumeLayout.
		protected int m_cDeepSuspendLayoutCount;
		protected SIL.Utils.StringTable m_stringTable;
		protected IPersistenceProvider m_persistenceProvider = null;
		protected FwStyleSheet m_styleSheet;
		protected bool m_fShowAllFields = false;
		protected ToolTip m_tooltip; // used for slice tree nodes. All tooltips are cleared when we switch records!
		protected LayoutStates m_layoutState = LayoutStates.klsNormal;
		protected int m_dxpLastRightPaneWidth = -1;  // width of right pane (if any) the last time we did a layout.
		// to allow slices to handle events (e.g. InflAffixTemplateSlice)
		protected XCore.Mediator m_mediator;
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
		/// affect what is done during OnLayout.
		/// </summary>
		public enum LayoutStates : byte
		{
			klsNormal, // OnLayout executes normally, nothing special is happening
			klsChecking, // OnPaint is checking that all slices that intersect the client area are ready to function.
			klsLayoutSuspended, // Had to suspend layout during paint, need to resume at end and repaint.
			klsClearingAll, // In the process of clearing all slices, ignore any intermediate layout messages.
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

		/// <summary>
		/// Private wrapper to remove the slices from the Datatree.
		/// </summary>
		private void ClearSlices()
		{
			LayoutStates oldState = m_layoutState;
			m_layoutState = LayoutStates.klsClearingAll;

			List<Slice> allSlices = new List<Slice>(Controls.Count);
			foreach (Slice slice in Controls)
			{
				slice.SplitCont.SplitterMoved -= new SplitterEventHandler(slice_SplitterMoved);
				allSlices.Add(slice);
				slice.AboutToDiscard();
			}

			// It's safer to set m_currentSlice to null here, so that we don't allow using
			// CurrentSlice during windows message side-effects that occur during Controls.Clear().
			m_currentSlice = null;
			Controls.Clear();

			if (m_tooltip != null)
				m_tooltip.RemoveAll(); // has many tooltips for old slices.

			foreach (Slice gonner in allSlices)
				gonner.Dispose();


			m_layoutState = oldState;
			AutoScrollPosition = new Point(0, 0);
		}

		private ToolTip ToolTip
		{
			get
			{
				CheckDisposed();

				if (m_tooltip == null)
				{
					m_tooltip = new ToolTip();
					m_tooltip.ShowAlways = true;
				}
				return m_tooltip;
			}
		}

		private void InsertSliceAndRegisterWithContextHelp(int index, Slice slice, XmlNode node)
		{
			RegisterWithContextHelp(slice, node);
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
			Debug.Assert(index >= 0 && index <= Controls.Count);

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
			sc.SplitterMoved -= new SplitterEventHandler(slice_SplitterMoved);
			sc.SplitterMoved += new SplitterEventHandler(slice_SplitterMoved);
		}

		/// <summary>
		/// For some strange reason, the first Controls.SetChildIndex doesn't always put it in the specified index.
		/// The second time seems to work okay though.
		/// </summary>
		/// <param name="slice"></param>
		/// <param name="index"></param>
		private void ForceSliceIndex(Slice slice, int index)
		{
			Controls.SetChildIndex(slice, index);
			if (slice.IndexInContainer != index)
				Controls.SetChildIndex(slice, index);
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
			Slice movedSlice = (sender is Slice) ?
				(sender as Slice) // sender is also a SplitContainer.
				: (sender as SplitContainer).Parent as Slice; // Have to move up one parent notch to get to teh Slice.
			if (m_currentSlice != movedSlice)
				return; // Too early to do much;

			Debug.Assert(movedSlice == m_currentSlice);

			m_sliceSplitPositionBase = movedSlice.SplitCont.SplitterDistance - movedSlice.LabelIndent();
			PersistPreferences();

			SuspendLayout();
			foreach (Slice otherSlice in Controls)
			{
				if (movedSlice != otherSlice)
				{
					SplitContainer otherSliceSC = otherSlice.SplitCont;
					// Remove and readd event handler when setting the value for the other fellow.
					otherSliceSC.SplitterMoved -= new SplitterEventHandler(slice_SplitterMoved);
					otherSlice.SetSplitPosition();
					otherSliceSC.SplitterMoved += new SplitterEventHandler(slice_SplitterMoved);
				}
			}
			ResumeLayout(false);
			// This can affect the lines between the slices. We need to redraw them but not the
			// slices themselves.
			Invalidate(false);
			movedSlice.TakeFocus();
		}

		protected void InsertSliceRange(int insertPosition, Set<Slice> slices)
		{
			List<Slice> indexableSlices = new List<Slice>(slices.ToArray());
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
		/// <param name="index"></param>
		/// <param name="slice"></param>
		internal void RawSetSlice(int index, Slice slice)
		{
			CheckDisposed();

			Debug.Assert(slice != Controls[index], "Can't replace the slice with itself.");

			RemoveSliceAt(index);
			InstallSlice(slice, index);
			SetTabIndex(index);
		}

		private void RemoveSliceAt(int index)
		{
			RemoveSlice(Controls[index] as Slice, index);
		}

		private void RemoveSlice(Slice gonner)
		{
			RemoveSlice(gonner, Controls.IndexOf(gonner));
		}

		private void RemoveSlice(Slice gonner, int index)
		{
			gonner.AboutToDiscard();
			gonner.SplitCont.SplitterMoved -= new SplitterEventHandler(slice_SplitterMoved);
			Controls.RemoveAt(index);

			// Reset CurrentSlice, if appropriate.
			if (gonner == m_currentSlice)
			{
				Slice newCurrent = null;
				if (Controls.Count > index)
				{
					// Get the one at the same index (next one after the one being removed).
					newCurrent = Controls[index] as Slice;
				}
				else if (Controls.Count > 0 && Controls.Count > index - 1)
				{
					// Get the one before index.
					newCurrent = Controls[index - 1] as Slice;
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
			bool gonnerHasToolTip = (gonner.ToolTip != null);
			if (gonnerHasToolTip)
				m_tooltip.RemoveAll();
			gonner.Dispose();
			// Now, we need to re-add all of the surviving tooltips.
			if (gonnerHasToolTip)
			{
				foreach (Slice keeper in Controls)
					SetToolTip(keeper);
			}

			ResetTabIndices(index);
		}

		private void SetTabIndex(int index)
		{
			Slice slice = Controls[index] as Slice;
			if (slice.IsRealSlice)
			{
				slice.TabIndex = index;
				if (slice.Control == null)
					slice.TabStop = false;
				else
					slice.TabStop = slice.Control.TabStop;
			}
		}

		/// <summary>
		/// Resets the TabIndex for all slices that are located at, or above, the <c>startingIndex</c>.
		/// </summary>
		/// <param name="startingIndex">The index to start renumbering the TabIndex.</param>
		private void ResetTabIndices(int startingIndex)
		{
			for (int i = startingIndex; i < Controls.Count; ++i)
				SetTabIndex(i);
		}

		#endregion Slice collection manipulation methods

		public DataTree()
		{
//			string objName = ToString() + GetHashCode().ToString();
//			Debug.WriteLine("Creating object:" + objName);

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
					m_styleSheet.Init(m_cache, m_cache.LangProject.LexDbOA.Hvo,
						(int)LexDb.LexDbTags.kflidStyles);
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
			if (m_monitoredProps.Contains(new KeyValuePair<int, int>(hvo, tag)))
			{
				RefreshList(false);
			}
			// Note, in LinguaLinks import we don't have an action handler when we hit this.
			else if (m_cache.ActionHandlerAccessor != null && m_cache.ActionHandlerAccessor.IsUndoOrRedoInProgress)
			{
				// Redoing an Add or Undoing a Delete may not have an existing slice to work with, so just force
				// a list refresh.  See LT-6033.
				if (hvo == m_hvoRoot)
				{
					int iType = m_mdc.GetFieldType((uint)tag);
					if (iType == (int)CellarModuleDefns.kcptOwningCollection ||
						iType == (int)CellarModuleDefns.kcptOwningSequence ||
						iType == (int)CellarModuleDefns.kcptReferenceCollection ||
						iType == (int)CellarModuleDefns.kcptReferenceSequence)
					{
						RefreshList(true);
						return;
					}
				}
				// some FieldSlices (e.g. combo slices)may want to Update their display
				// if its field changes during an Undo/Redo (cf. LT-4861).
				RefreshList(hvo, tag);
			}
		}

		public XCore.Mediator Mediator
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
					m_currentSlice.SetCurrentState(false);

				m_currentSlice = value;

				// Tell new guy it is current.
				m_currentSlice.SetCurrentState(true);

				int index = m_currentSlice.IndexInContainer;
				this.ScrollControlIntoView(m_currentSlice);

				// All subsequent slices are not active.
				for (int i = index + 1; i < Controls.Count; i++)
				{
					Slice slice = Controls[i] as Slice;
					if (slice != null)
						slice.Active = false;
				}
				for (int i = 0; i <= index; i++)
				{
					Slice slice = Controls[i] as Slice;
					if (slice != null)
						slice.BecomeActiveIfParent(m_currentSlice);
				}
				// Ensure that we can tab and shift-tab. This requires that at least one
				// following and one prior slice be a tab stop, if possible.
				for (int i = index + 1; i < Controls.Count; i++)
				{
					MakeSliceRealAt(i);
					if (Controls[i].TabStop)
						break;
				}
				for (int i = index - 1; i >= 0; i--)
				{
					MakeSliceRealAt(i);
					if (Controls[i].TabStop)
						break;
				}
				this.Invalidate();	// .Refresh();
			}
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
				int i = Controls.Count - 1;
				for (; i >= 0; --i)
				{
					Slice current = Controls[i] as Slice;
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
				if (i >= 0 && i < Controls.Count)
					return true;
				else
					return false;
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

		public void Reset()
		{
			CheckDisposed();

			m_root = null;
			m_hvoRoot = 0;
		}

		private void ResetRecordListUpdater()
		{
			if (m_listName != null && m_rlu == null)
			{
				// Find the first parent IRecordListOwner object (if any) that
				// owns an IRecordListUpdater.
				IRecordListOwner rlo = m_mediator.PropertyTable.GetValue("window") as IRecordListOwner;
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
				foreach (Slice slice in Controls)
				{
					SplitContainer sc = slice.SplitCont;
					sc.SplitterMoved -= new SplitterEventHandler(slice_SplitterMoved);
					slice.SetSplitPosition();
					sc.SplitterMoved += new SplitterEventHandler(slice_SplitterMoved);
				}
				ResumeLayout(false);
				// This can affect the lines between the slices. We need to redraw them but not the
				// slices themselves.
				this.Invalidate(false);
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
				else if (m_mediator != null)
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

		public virtual void ShowObject(int hvoRoot, string layoutName)
		{
			CheckDisposed();

			if (m_hvoRoot == hvoRoot && layoutName == m_rootLayoutName)
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
				m_currentSliceObjHvo = m_mediator.PropertyTable.GetIntProperty(m_sObjHvoProperty, 0, PropertyTable.SettingsGroup.LocalSettings);
				m_currentSliceNew = null;
				m_fSetCurrentSliceNew = false;
			}

			DeepSuspendLayout();
			try
			{
				m_rootLayoutName = layoutName;
				Debug.Assert(m_cache != null, "You need to call Initialize() first.");

				if (m_hvoRoot != hvoRoot)
				{
					// A reasonably assumption is that no slice will be reusable for a different
					// object, and the 'path' that is used to figure whether a slice IS reusable
					// doesn't include the root, so if we attempt reuseMap, we will get spurious
					// matches. So discard all slices.

					// Use the safe way now.  This may go away if the SliceCollection gets refactored
					// out in the near future .. (that'd be nice.)
					ClearSlices();

					m_hvoRoot = hvoRoot;
					m_root = CmObject.CreateFromDBObject(m_cache, m_hvoRoot);
					if (m_rch != null)
					{
						// We need to refresh the record list if homograph numbers change.
						// Do it for the old object.
						m_rch.Fixup(true);
						// Root has changed, so reset the handler.
						m_rch.Setup(m_root, m_rlu);
					}
					Invalidate(); // clears any lines left over behind slices.
					CreateSlices();
				}
				else
				{
					RefreshList(false);  // This could be optimized more, too, but it isn't the common case.
				}

				// We can't focus yet because the data tree slices haven't finished displaying.
				// (See LT-3915.)  So postpone focusing by posting a message...
				// Mediator may be null during testing or maybe some other strange state
				if (m_mediator != null)
				{
					m_mediator.PostMessage("ReadyToSetCurrentSlice", this);
					// prevent setting focus in slice until we're all setup (cf.
					m_fSuspendSettingCurrentSlice = true;
				}
			}
			finally
			{
				this.DeepResumeLayout();
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
			if (String.IsNullOrEmpty(m_sPartNameProperty) || String.IsNullOrEmpty(m_sObjHvoProperty))
			{
				if (m_mediator != null)
				{
					string sTool = m_mediator.PropertyTable.GetStringProperty("currentContentControl", String.Empty);
					string sArea = m_mediator.PropertyTable.GetStringProperty("areaChoice", String.Empty);
					m_sPartNameProperty = String.Format("{0}${1}$CurrentSlicePartName", sArea, sTool);
					m_sObjHvoProperty = String.Format("{0}${1}$CurrentSliceObjectHvo", sArea, sTool);
				}
				else
				{
					m_sPartNameProperty = "$$CurrentSlicePartName";
					m_sObjHvoProperty = "$$CurrentSliceObjectHvo";
				}
			}
		}

		public int RootObjectHvo
		{
			get
			{
				CheckDisposed();

				return m_hvoRoot;
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
		/// <param name="cache"></param>
		/// <param name="fHasSplitter"></param>
		protected void InitializeBasic(FDO.FdoCache cache, bool fHasSplitter)
		{
			//in a normal user application, this auto menu handler will not be used.
			//instead, the client of this control will call SetContextMenuHandler()
			//with a customized handler.
			// m_autoHandler = new AutoDataTreeMenuHandler(this);
			// we never use auto anymore			SetContextMenuHandler(new SliceShowMenuRequestHandler(m_autoHandler.GetSliceContextMenu));

			// This has to be created before we start adding slices, so they can be put into it.
			// (Otherwise we would normally do this in initializeComponent.)
			m_fHasSplitter = fHasSplitter;
			m_mdc = cache.MetaDataCacheAccessor;
			m_cache = cache;
		}

		/// <summary>
		/// This is the initialize that is normally used. Others may not be extensively tested.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="fHasSplitter"></param>
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
			m_sda = m_cache.MainCacheAccessor;
			m_sda.AddNotification(this);

			// Currently we inherit from UserControl, which doesn't have a border. If we
			// need one various things will have to change to Panel.
			//this.BorderStyle = BorderStyle.FixedSingle;
			this.BackColor = Color.FromKnownColor(KnownColor.Window);
		}

		/// <summary>
		///
		/// </summary>
		protected void InitializeComponent()
		{
			InitializeComponentBasic();
			try
			{
				DeepSuspendLayout();
				// NB: The ArrayList created here can hold disparate objects, such as XmlNodes and ints.
				if (m_root != null)
					CreateSlicesFor(m_root, null, 0, 0, new ArrayList(20), new ObjSeqHashMap(), null);
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
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			// m_sda COM object block removed due to crash in Finializer thread LT-6124

			if (disposing)
			{
				// Do this first, before setting m_fDisposing to true.
				if (m_sda != null)
					m_sda.RemoveNotification(this);

				m_fDisposing = true; // 'Disposing' isn't until we call base dispose.
				m_currentSlice = null;
				if (m_rch != null)
				{
					if (m_rch.HasRecordListUpdater)
					{
					m_rch.Fixup(false);		// no need to refresh record list on shutdown.
					}
					else if (m_rch is IDisposable)
					{
						// It's fine to dispose it, after all, because m_rch has no other owner.
						(m_rch as IDisposable).Dispose();
					}
				}
				if (m_tooltip != null)
				{
					m_tooltip.RemoveAll();
					m_tooltip.Dispose();
				}
				foreach (Slice slice in Controls)
					slice.ShowContextMenu -= new TreeNodeEventHandler(this.OnShowContextMenu);
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
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		private void RefreshList(int hvo, int tag)
		{
			foreach (Slice slice in Controls)
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
		/// <param name="clearAllSlices">
		/// True to not recycle any slices.
		/// False to try and recycle them.
		/// </param>
		/// <remarks>
		/// If the DataTree's slices call this method, they should use 'false',
		/// or they will be disposed when this call returns to them.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public virtual void RefreshList(bool clearAllSlices)
		{
			CheckDisposed();
			if (m_fDoNotRefresh)
			{
				RefreshListNeeded = true;
				m_fPostponedClearAllSlices |= clearAllSlices;
				return;
			}
			Form myWindow = FindForm();
			myWindow.Cursor = Cursors.WaitCursor;
			try
			{
				Slice oldCurrent = m_currentSlice;
				DeepSuspendLayout();

				m_currentSlicePartName = String.Empty;
				m_currentSliceObjHvo = 0;
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
						m_currentSliceObjHvo = m_currentSlice.Object.Hvo;
					xnConfig = m_currentSlice.ConfigurationNode;
					xnCaller = m_currentSlice.CallerNode;
					sLabel = m_currentSlice.Label;
					oldType = m_currentSlice.GetType();
				}
				if (clearAllSlices)
					ClearSlices(); // Don't even try to recycle them.

				// Make sure we invalidate the root object if it's been deleted.
				if (m_root != null && (m_cache.IsDummyObject(m_hvoRoot) || !m_cache.IsValidObject(m_hvoRoot)))
				{
					Reset();
				}

				// Make a new root object...just in case it changed class.
				if (m_root != null)
					m_root = CmObject.CreateFromDBObject(m_root.Cache, m_root.Hvo);

				Invalidate(true); // forces all children to invalidate also
				CreateSlices();
				PerformLayout();

				if (Controls.Contains(oldCurrent))
				{
					CurrentSlice = oldCurrent;
					if (CurrentSlice != oldCurrent)
						m_currentSliceNew = oldCurrent;
					else
						m_currentSliceNew = null;
				}
				else if (oldCurrent != null)
				{
					foreach (Control ctl in Controls)
					{
						Slice slice = ctl as Slice;
						if (slice == null)
							continue;
						int hvoSlice = 0;
						if (slice.Object != null)
							hvoSlice = slice.Object.Hvo;
						if (slice.GetType() == oldType &&
							slice.CallerNode == xnCaller &&
							slice.ConfigurationNode == xnConfig &&
							hvoSlice == m_currentSliceObjHvo &&
							slice.Label == sLabel)
						{
							CurrentSlice = slice;
							if (CurrentSlice != slice)
								m_currentSliceNew = slice;
							else
								m_currentSliceNew = null;
							break;
						}
					}
				}

				if (m_currentSlice != null)
				{
					ScrollControlIntoView(m_currentSlice);
				}
			}
			finally
			{
				DeepResumeLayout();
				RefreshListNeeded = false;  // reset our flag.
				myWindow.Cursor = Cursors.Default;
				m_currentSlicePartName = null;
				m_currentSliceObjHvo = 0;
				m_fSetCurrentSliceNew = false;
				if (m_currentSliceNew != null)
				{
					m_mediator.PostMessage("ReadyToSetCurrentSlice", this);
					// prevent setting focus in slice until we're all setup (cf.
					m_fSuspendSettingCurrentSlice = true;
				}
			}
		}

		/// <summary>
		/// Create slices appropriate for current root object and layout, reusing any existing slices,
		/// and clearing out any that remain unused.
		/// </summary>
		private void CreateSlices()
		{
			m_currentSlice = null;
			ObjSeqHashMap previousSlices = new ObjSeqHashMap();
			List<Slice> dummySlices = new List<Slice>(Controls.Count);
			foreach (Slice slice in Controls)
			{
				if (slice.Key != null) // dummy slices may not have keys and shouldn't be reused.
					previousSlices.Add(slice.Key, slice);
				else
					dummySlices.Add(slice);
			}
			// Get rid of the dummies we aren't going to remove.
			foreach (Slice slice in dummySlices)
				RemoveSlice(slice);
			CreateSlicesFor(m_root, m_rootLayoutName, 0, 0, new ArrayList(20), previousSlices, null);
			// Clear out any slices NOT reused. Removing them from the collection both
			// removes them from the DataTree's controls collection and disposes them.
			foreach (IList sliceList in previousSlices.Values)
			{
				foreach (Slice gonner in sliceList)
				{
					RemoveSlice(gonner);
				}
			}
			ResetTabIndices(0);
		}

		/// <summary>
		/// This is called (by reflection) when the user issues a Refresh command.
		/// All data will have been cleared from the cache, so some additional preloading may
		/// be helpful.
		/// </summary>
		public virtual void RefreshDisplay()
		{
			CheckDisposed();

			RefreshList(false);
		}

		/// <summary>
		/// Answer true if the two slices are displaying fields of the same object.
		/// Review: should we require more strictly, that the full path of objects in their keys are the same?
		/// </summary>
		/// <param name="first"></param>
		/// <param name="second"></param>
		/// <returns></returns>
		bool SameSourceObject(Slice first, Slice second)
		{
			return first.Object.Hvo == second.Object.Hvo;
		}

		/// <summary>
		/// Answer true if the second slice is a 'child' of the first (common key)
		/// </summary>
		/// <param name="first"></param>
		/// <param name="second"></param>
		/// <returns></returns>
		bool IsChildSlice(Slice first, Slice second)
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
		/// <param name="sender"></param>
		/// <param name="pea"></param>
		void HandlePaintLinesBetweenSlices(object sender, PaintEventArgs pea)
		{
			Graphics gr = pea.Graphics;
			UserControl uc = this; // Where we're drawing.
			int width = uc.Width;
			Pen thinPen = new Pen(Color.LightGray, 1);
			Pen thickPen = new Pen(Color.LightGray, 1 + HeavyweightRuleThickness);

			for (int i = 0; i < Controls.Count; i++)
			{
				Slice slice = Controls[i] as Slice;
				if(slice == null)
					continue;  // shouldn't be visible
				Slice nextSlice = null;
				if (i < Controls.Count - 1)
					nextSlice = Controls[i + 1] as Slice;
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
						yPos += DataTree.HeavyweightRuleAboveMargin; //jh added
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
		/// <param name="obj"> The object to make slices for.</param>
		/// <param name="mode">If not null, use only templates having this mode.</param>
		/// <param name="level">level at which to put the new top-level slices</param>
		/// <param name="preceding">index after which to insert the new slice(s) in slices array.</param>
		/// <param name="path">sequence of nodes and HVOs inside which this is nested</param>
		/// <param name="reuseMap">map of key/slice combinations from a DataTree being refreshed. Exact matches may be
		/// reused, and also, the expansion state of exact matches is preserved.</param>
		/// <param name="unifyWith">If not null, this is a node to be 'unified' with the one looked up
		/// using the layout name.</param>
		/// <returns> updated insertPosition for next item after the ones inserted.</returns>
		public virtual int CreateSlicesFor(ICmObject obj, string layoutName, int indent,
			int insertPosition, ArrayList path, ObjSeqHashMap reuseMap, XmlNode unifyWith)
		{
			CheckDisposed();

			// NB: 'path' can hold either ints or XmlNodes, so a generic can't be used for it.
			if (obj == null)
				return insertPosition;
			XmlNode template = GetTemplateForObjLayout(obj, layoutName);
			path.Add(template);
			XmlNode template2 = template;
			if (unifyWith != null && unifyWith.ChildNodes.Count > 0)
			{
				// This assumes that the attributes don't need to be unified.
				template2 = m_layoutInventory.GetUnified(template, unifyWith);
			}
			insertPosition = ApplyLayout(obj, template2, indent, insertPosition, path, reuseMap);
			path.RemoveAt(path.Count - 1);
			return insertPosition;
		}

		/// <summary>
		/// Get the template that should be used to display the specified object using the specified layout.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="layoutName"></param>
		/// <param name="classname"></param>
		/// <returns></returns>
		private XmlNode GetTemplateForObjLayout(ICmObject obj, string layoutName)
		{
			uint classId = (uint)obj.ClassID;
			string classname;
			XmlNode template = null;
			string useName = layoutName == null ? "default" : layoutName;
			string origName = useName;
			for( ; ; )
			{
				classname = m_mdc.GetClassName(classId);
				// Inventory of layouts has keys class, type, name
				template = m_layoutInventory.GetElement("layout", new string[] {classname, "detail", useName});
				if (template != null)
					break;
				if (classId == 0 && useName != "default")
				{
					// Nothing found all the way to CmObject...try default view.
					useName = "default";
					classId = (uint) obj.ClassID;
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
			return (int)mdc.GetClassId(stClassName);
		}

		/// <summary>
		/// Look for a reusable slice that matches the current path. If found, remove from map and return;
		/// otherwise, return null.
		/// </summary>
		private Slice GetMatchingSlice(ArrayList path, ObjSeqHashMap reuseMap)
		{
			// Review JohnT(RandyR): I don't see how this can really work.
			// The original path (the key) used to set this does not, (and cannot) change,
			// but it is very common for slices to come and go, as they are inserted/deleted,
			// or when the Show hidden control is changed.
			// Those kinds of big changes will produce the input 'path' parm,
			// which has little hope of matching that fixed orginal key, won't it.
			// I can see how it would work when a simple F4 refresh is being done,
			// since the count of slcies should remain the same.

			IList list = reuseMap[(IList)path];
			if (list.Count > 0)
			{
				Slice slice = (Slice)list[0];
				reuseMap.Remove(path, slice);
				return slice;
			}

			return null;
		}
		public enum NodeTestResult : int
		{
			kntrSomething, // really something here we could expand
			kntrPossible, // nothing here, but there could be
			kntrNothing // nothing could possibly be here, don't show collapsed OR expanded.
		}

		/// <summary>
		/// Apply a layout to an object, producing the specified slices.
		/// </summary>
		/// <param name="obj">The object we want a detai view of</param>
		/// <param name="template">the 'layout' element</param>
		/// <param name="indent">How deeply indented the tree is at this point.</param>
		/// <param name="insertPosition">index in slices where we should insert nodes</param>
		/// <param name="path">sequence of nodes and HVOs inside which this is nested</param>
		/// <param name="reuseMap">map of key/slice combinations from a DataTree being refreshed. Exact matches may be
		/// reused, and also, the expansion state of exact matches is preserved.</param>
		/// <returns> updated insertPosition for next item after the ones inserted.</returns>
		public int ApplyLayout(ICmObject obj, XmlNode template, int indent, int insertPosition,
			ArrayList path, ObjSeqHashMap reuseMap)
		{
			CheckDisposed();
			NodeTestResult ntr;
			return ApplyLayout(obj, template, indent, insertPosition, path, reuseMap, false, out ntr);
		}

		/// <summary>
		/// This is the guts of ApplyLayout, but it has extra arguments to allow it to be used both to actually produce
		/// slices, and just to query whether any slices will be produced.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="template"></param>
		/// <param name="indent"></param>
		/// <param name="insertPosition"></param>
		/// <param name="path"></param>
		/// <param name="reuseMap"></param>
		/// <param name="fTestOnly"></param>
		/// <param name="fGotAnything"></param>
		/// <returns></returns>
		protected internal virtual int ApplyLayout(ICmObject obj, XmlNode template, int indent, int insertPosition, ArrayList path, ObjSeqHashMap reuseMap,
			bool isTestOnly, out NodeTestResult testResult)
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
				if(XmlUtils.GetOptionalAttributeValue(partRef, "customFields", null) != null)
				{
					if(!isTestOnly)
						EnsureCustomFields(obj, template, partRef);

					continue;
				}

				testResult = ProcessPartRefNode(partRef, path, reuseMap, obj, indent, ref insPos,
					isTestOnly);

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
		/// Currently these are all part nodes (with ref= indicating the part to use).
		/// </summary>
		/// <param name="part"></param>
		/// <param name="path"></param>
		/// <param name="reuseMap"></param>
		/// <param name="obj"></param>
		/// <param name="indent"></param>
		/// <param name="insPos"></param>
		/// <param name="isTestOnly"></param>
		/// <returns>NodeTestResult</returns>
		private NodeTestResult ProcessPartRefNode(XmlNode partRef, ArrayList path, ObjSeqHashMap reuseMap,
			ICmObject obj, int indent, ref int insPos, bool isTestOnly)
		{
			// Use the part inventory to find the indicated part.
			Debug.Assert(partRef.Name == "part");
			// If the previously selected slice doesn't display in this refresh, we try for the next
			// visible slice instead.  So m_fSetCurrentSliceNew might still be set.  See LT-9010.
			string partName = XmlUtils.GetManditoryAttributeValue(partRef, "ref");
			if (!m_fSetCurrentSliceNew && m_currentSlicePartName != null && obj.Hvo == m_currentSliceObjHvo)
			{
				for (uint clid = (uint)obj.ClassID; clid != 0; clid = m_mdc.GetBaseClsId(clid))
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

			uint classId = (uint)obj.ClassID;
			string classname;
			XmlNode part = null;
			for( ; ; )
			{
				classname = m_mdc.GetClassName(classId);
				// Inventory of parts has key ID. The ID is made up of the class name, "-Detail-", partname.
				string key = classname + "-Detail-"+partName;
				part = m_partInventory.GetElement("part", new string[] {key});
				int temp = 0;
				if (part != null)
					temp = part.GetHashCode();
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
			NodeTestResult ntr = ProcessPartChildren(part, path, reuseMap, obj, indent, ref insPos, isTestOnly,
				parameter, visibility=="ifdata", partRef);
			path.RemoveAt(path.Count - 1);
			return ntr;
		}

		internal NodeTestResult ProcessPartChildren(XmlNode part, ArrayList path,
			ObjSeqHashMap reuseMap, ICmObject obj, int indent, ref int insPos, bool isTestOnly,
			string parameter, bool fVisIfData, XmlNode caller)
		{
			CheckDisposed();
			// The children of the part element must now be processed. Often there is only one.
			foreach (XmlNode node in part.ChildNodes)
			{
				if (node.GetType() == typeof(XmlComment))
					continue;
				NodeTestResult testResult = ProcessSubpartNode(node, path, reuseMap, obj,
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
		/// <param name="obj"></param>
		/// <param name="template"></param>
		private void EnsureCustomFields(ICmObject obj, XmlNode template, XmlNode insertAfter)
		{
			Set<int> interestingClasses = new Set<int>();
			int clsid = obj.ClassID;
			while (clsid != 0)
			{
				interestingClasses.Add(clsid);
				clsid = (int)obj.Cache.MetaDataCacheAccessor.GetBaseClsId((uint) clsid);
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
						if (checkCustomFieldsSibling(sibling, target))
							exists = true;
					}
					for(XmlNode sibling = insertAfter.PreviousSibling; sibling != null && !exists;	sibling = sibling.PreviousSibling)
					{
						if (checkCustomFieldsSibling(sibling, target))
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

		private bool checkCustomFieldsSibling(XmlNode sibling, string target)
		{
			if (sibling.Attributes == null)
				return false;	// no attributes on this nodeas XmlComment  LT-3566

			XmlNode paramAttr = sibling.Attributes["param"];
			XmlNode refAttr = sibling.Attributes["ref"];
			if (paramAttr != null && refAttr != null && paramAttr.Value == target && sibling.Name == "part" && refAttr.Value == "Custom")
				return true;
			else
				return false;
		}

		private XmlNode MakeCustomFieldNode(XmlDocument document, FDO.FieldDescription field)
		{
			XmlNode node = document.CreateElement("slice");
			AddAttribute(node, "field", field.Name);//e.g. "custom", "custom1", "custom2"
			AddAttribute(node, "classId", field.Class.ToString());
			AddAttribute(node, "label", field.Userlabel);
			AddAttribute(node, "menu", "mnuDataTree-Help");

			string strWs = "";
			string strEditor = "";
			switch(field.WsSelector)
			{
				case LangProject.kwsAnal:
					strWs = "analysis";
					strEditor = "string";
					break;
				case LangProject.kwsVern:
					strWs = "vernacular";
					strEditor = "string";
					break;
				case LangProject.kwsVerns:
					strWs = "all vernacular";
					strEditor = "multistring";
					break;
				case LangProject.kwsAnals:
					strWs = "all analysis";
					strEditor = "multistring";
					break;
				case LangProject.kwsAnalVerns:
					strWs = "analysis vernacular";
					strEditor = "multistring";
					break;
				case LangProject.kwsVernAnals:
					strWs = "vernacular analysis";
					strEditor = "multistring";
					break;
			}
			AddAttribute(node, "editor", strEditor);
			AddAttribute(node, "ws", strWs);
			return node;
		}

		private void AddAttribute(XmlNode node, string name, string value)
		{
			XmlAttribute attribute= node.OwnerDocument.CreateAttribute(name);
			attribute.Value=value;
			node.Attributes.Append(attribute);
		}

		/// <summary>
		/// Handle one (non-comment) child node of a template (or other node) being used to
		/// create slices.  Update insertPosition to indicate how many were added (it also
		/// specifies where to add).  If fTestOnly is true, do not update insertPosition, just
		/// return true if any slices would be created.  Note that this method is recursive
		/// indirectly through ProcessPartChildren().
		/// </summary>
		/// <param name="node"></param>
		/// <param name="path"></param>
		/// <param name="reuseMap"></param>
		/// <param name="obj"></param>
		/// <param name="indent"></param>
		/// <param name="insertPosition"></param>
		/// <param name="fTestOnly"></param>
		/// <param name="parameter"></param>
		/// <param name="fVisIfData">If true, show slice only if data present.</param>
		/// <returns></returns>
		private NodeTestResult ProcessSubpartNode(XmlNode node, ArrayList path,
			ObjSeqHashMap reuseMap, ICmObject obj, int indent, ref int insertPosition,
			bool fTestOnly, string parameter, bool fVisIfData, XmlNode caller)
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
						break; // Nothing to do for unrecognized element, such as deParams.

					case "slice":
						return AddSimpleNode(path, node, reuseMap, editor, flid, obj, indent,
							ref insertPosition, fTestOnly, fVisIfData, caller);

					case "seq":
						return AddSeqNode(path, node, reuseMap, editor, flid, obj, indent + Slice.ExtraIndent(node),
							ref insertPosition, fTestOnly, parameter, fVisIfData, caller);

					case "obj":
						return AddAtomicNode(path, node, reuseMap, editor, flid, obj, indent  + Slice.ExtraIndent(node),
							ref insertPosition, fTestOnly, parameter, fVisIfData, caller);

					case "if":
						if (XmlVc.ConditionPasses(node, obj.Hvo, m_cache))
						{
							NodeTestResult ntr = ProcessPartChildren(node, path, reuseMap, obj,
								indent, ref insertPosition, fTestOnly, parameter, fVisIfData,
								caller);
							if (fTestOnly && ntr != NodeTestResult.kntrNothing)
								return ntr;
						}
						break;

					case "ifnot":
						if (!XmlVc.ConditionPasses(node, obj.Hvo, m_cache))
						{
							NodeTestResult ntr = ProcessPartChildren(node, path, reuseMap, obj,
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
										reuseMap, obj, indent, ref insertPosition, fTestOnly,
										parameter, fVisIfData, caller);
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
									reuseMap, obj, indent, ref insertPosition, fTestOnly,
									parameter, fVisIfData, caller);
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
						if (m_rch != null && !m_rch.HasRecordListUpdater && m_rch is IDisposable)
						{
							// The above version of the Dispose call was bad,
							// when m_rlu 'owned' the m_rch.
							// Now, we know there is no 'owning' m_rlu, so we have to do it.
							(m_rch as IDisposable).Dispose();
							m_rch = null;
						}
						m_rch = (IRecordChangeHandler)DynamicLoader.CreateObject(node, null);
						m_rch.Disposed += new EventHandler(m_rch_Disposed);
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
				string s = "FieldWorks ran into a problem trying to display this object";
				s += " in DataTree::ApplyLayout: " + error.Message;
				s += "\r\nThe object id was " + obj.Hvo.ToString() + ".";
				if (editor != null)
					s += " The editor was '" + editor + "'.\r\n";
				s += " The text of the current node was " + node.OuterXml;
				//now send it on
				throw new ApplicationException(s, error);
			}
			// other types of child nodes, for example, parameters for jtview, don't even have
			// the potential for expansion.
			return NodeTestResult.kntrNothing;
		}

		void m_rch_Disposed(object sender, EventArgs e)
		{
			// It was disposed, so clear out the data member.
			if (m_rch != null)
				m_rch.Disposed -= new EventHandler(m_rch_Disposed);
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
				flid = (int)m_mdc.GetFieldId2((uint)obj.ClassID, attrName, true);
				if (flid == 0)
					throw new ApplicationException(
						"DataTree could not find the flid for attribute '" + attrName +
						"' of class '" + obj.ClassID + "'.");
			}
			return flid;
		}

		private NodeTestResult AddAtomicNode(ArrayList path, XmlNode node, ObjSeqHashMap reuseMap, string editor,
			int flid, ICmObject obj, int indent, ref int insertPosition, bool fTestOnly,
			string layoutName, bool fVisIfData, XmlNode caller)
		{
			// Facilitate insertion of an expandable tree node representing an owned or ref'd object.
			if (flid == 0)
				throw new ApplicationException("field attribute required for atomic properties " + node.OuterXml);
			ICmObject innerObj = obj.GetObjectInAtomicField(flid);
			m_monitoredProps.Add(new KeyValuePair<int, int>(obj.Hvo, flid));
			if (fVisIfData && innerObj == null)
				return NodeTestResult.kntrNothing;
			if (fTestOnly)
			{
				if (innerObj != null || XmlUtils.GetOptionalAttributeValue(node, "ghost") != null)
					return NodeTestResult.kntrSomething;
				else
					return NodeTestResult.kntrPossible;
			}
			path.Add(node);
			if(innerObj != null)
			{
				string layoutOverride = XmlUtils.GetOptionalAttributeValue(node, "layout", layoutName);
				path.Add(innerObj.Hvo);
				insertPosition = CreateSlicesFor(CmObject.CreateFromDBObject(m_cache, innerObj.Hvo),
					layoutOverride, indent, insertPosition, path, reuseMap, caller);
				path.RemoveAt(path.Count - 1);
			}
			else
			{
				// No inner object...do we want a ghost slice?
				if (XmlUtils.GetOptionalAttributeValue(node, "ghost") != null)
				{
					MakeGhostSlice(path, node, reuseMap, obj, flid, caller, indent, ref insertPosition);
				}
			}
			path.RemoveAt(path.Count - 1);
			return NodeTestResult.kntrNothing;
		}

		void MakeGhostSlice(ArrayList path, XmlNode node, ObjSeqHashMap reuseMap, ICmObject obj,
			int flidEmptyProp, XmlNode caller, int indent, ref int insertPosition)
		{
			// It's a really bad idea to add it to the path, since it kills
			// the code that hot swaps it, when becoming real.
			//path.Add(node);
			Slice slice = GetMatchingSlice(path, reuseMap);
			if (slice == null)
			{
				slice = new GhostStringSlice(obj.Hvo, flidEmptyProp, node, m_cache);
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
				slice.ShowContextMenu += new TreeNodeEventHandler(this.OnShowContextMenu);

				slice.SmallImages = SmallImages;
				SetNodeWeight(node, slice);

				slice.FinishInit();
				InsertSliceAndRegisterWithContextHelp(insertPosition, slice, node);
			}
			else
			{
				EnsureValidIndexForReusedSlice(slice, insertPosition);
			}
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
		private List<int> m_currentObjectFlids = new List<int>();
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
			for (int hvo = hvoOwner; hvo != 0; hvo = m_cache.GetOwnerOfObject(hvo))
			{
				int flid = m_cache.GetOwningFlidOfObject(hvo);
				if (flid != 0)
					m_currentObjectFlids.Add(flid);
			}
		}
		internal void ClearCurrentObjectFlids()
		{
			m_currentObjectFlids.Clear();
		}

		private NodeTestResult AddSeqNode(ArrayList path, XmlNode node, ObjSeqHashMap reuseMap, string editor,
			int flid, ICmObject obj, int indent, ref int insertPosition, bool fTestOnly,
			string layoutName, bool fVisIfData, XmlNode caller)
		{
			if (flid == 0)
				throw new ApplicationException("field attribute required for seq properties " + node.OuterXml);
			int cobj = m_cache.GetVectorSize(obj.Hvo, flid);
			// monitor it even if we're testing: result may change.
			m_monitoredProps.Add(new KeyValuePair<int, int>(obj.Hvo, flid));
			if (fVisIfData && cobj == 0)
				return NodeTestResult.kntrNothing;
			if (fTestOnly)
			{
				if (cobj > 0 || XmlUtils.GetOptionalAttributeValue(node, "ghost") != null)
					return NodeTestResult.kntrSomething;
				else
					return NodeTestResult.kntrPossible;
			}
			path.Add(node);
			string layoutOverride = XmlUtils.GetOptionalAttributeValue(node, "layout", layoutName);
			if (cobj == 0)
			{
				// Nothing in seq....do we want a ghost slice?
				if (XmlUtils.GetOptionalAttributeValue(node, "ghost") != null)
				{
					MakeGhostSlice(path, node, reuseMap, obj, flid, caller, indent, ref insertPosition);
				}
			}
			else if (cobj < 15 ||	// This may be a little on the small side
				m_currentObjectFlids.Contains(flid) ||
				(!String.IsNullOrEmpty(m_currentSlicePartName) && m_currentSliceObjHvo != 0 && m_currentSliceNew == null))
			{
				// Create slices immediately
				foreach (int hvo in m_cache.GetVectorProperty(obj.Hvo, flid, false))
				{
					path.Add(hvo);
					insertPosition = CreateSlicesFor(CmObject.CreateFromDBObject(m_cache, hvo),
						layoutOverride, indent, insertPosition, path, reuseMap, caller);
					path.RemoveAt(path.Count - 1);
				}
			}
			else
			{
				// Create unique DummyObjectSlices for each slice.  This may reduce the initial
				// preceived benefit, but this way doesn't crash now that the slices are being
				// disposed of.
				int cnt = 0;
				foreach (int hvo in m_cache.GetVectorProperty(obj.Hvo, flid, false))
				{
					path.Add(hvo);
					DummyObjectSlice dos = new DummyObjectSlice(this, indent, node, (ArrayList)(path.Clone()),
						obj, flid, cnt, layoutOverride, caller);
					dos.Cache = m_cache;
					InsertSlice(insertPosition++, dos);
					////InstallSlice(dos, -1);
					////ResetTabIndices(insertPosition);
					////insertPosition++;
					path.RemoveAt(path.Count - 1);
					cnt++;
				}
			}
			path.RemoveAt(path.Count - 1);
			return NodeTestResult.kntrNothing;
		}

		/// <summary>
		/// This parses the label attribute in order to return a label from a specified field name.
		/// Currently only recognizes "$owner" to recognize the owning object, this could be expanded
		/// to include $obj or other references.
		/// </summary>
		/// <param name="label"></param>
		/// <returns></returns>
		internal string InterpretLabelAttribute(string label, ICmObject obj)
		{
			CheckDisposed();
			if (label != null && label.Length > 7 && label.Substring(0,7).ToLower() == "$owner.")
			{
				string subfield = label.Substring(7);
				ICmObject owner = CmObject.CreateFromDBObject(Cache, obj.OwnerHVO);
				IFwMetaDataCache mdc = Cache.MetaDataCacheAccessor;
				int flidSubfield = (int)mdc.GetFieldId2((uint)owner.ClassID, subfield, true);
				if (flidSubfield != 0)
				{
					FieldType type = Cache.GetFieldType(flidSubfield);
					switch (type)
					{
					default:
						Debug.Assert(type == FieldType.kcptUnicode);
						break;
					case FieldType.kcptMultiString:
					case FieldType.kcptMultiBigString:
						label = Cache.GetMultiStringAlt(owner.Hvo, flidSubfield, Cache.DefaultAnalWs).Text;
						break;
					case FieldType.kcptMultiUnicode:
					case FieldType.kcptMultiBigUnicode:
						label = Cache.GetMultiUnicodeAlt(owner.Hvo, flidSubfield, Cache.DefaultAnalWs, null);
						break;
					case FieldType.kcptString:
					case FieldType.kcptBigString:
						label = Cache.GetTsStringProperty(owner.Hvo, flidSubfield).Text;
						break;
					case FieldType.kcptUnicode:
					case FieldType.kcptBigUnicode:
						label = Cache.GetUnicodeProperty(owner.Hvo, flidSubfield);
						break;
					}
				}
			}
			return label;
		}

		/// <summary>
		/// Tests to see if it should add the field (IfData), then adds the field.
		/// </summary>
		/// <param name="path"></param>
		/// <param name="node"></param>
		/// <param name="reuseMap"></param>
		/// <param name="editor">Type of Contained Data</param>
		/// <param name="flid">Field ID</param>
		/// <param name="obj"></param>
		/// <param name="indent"></param>
		/// <param name="insPos"></param>
		/// <param name="fTestOnly"></param>
		/// <param name="fVisIfData">IfData</param>
		/// <param name="caller"></param>
		/// <returns>NodeTestResult, an enum showing if usable data is contained in the field</returns>
		private NodeTestResult AddSimpleNode(ArrayList path, XmlNode node, ObjSeqHashMap reuseMap, string editor,
			int flid, ICmObject obj, int indent, ref int insPos, bool fTestOnly, bool fVisIfData, XmlNode caller)
		{
			if (fVisIfData) // Contains the tests to see if usable data is inside the field (for all types of fields)
			{
				if (editor != null && editor == "custom")
				{
					System.Type typeFound;
					System.Reflection.MethodInfo mi =
						XmlUtils.GetStaticMethod(node, "assemblyPath", "class", "ShowSliceForVisibleIfData", out typeFound);
					if (mi != null)
					{
						object[] parameters = new object[2];
						parameters[0] = (object)node;
						parameters[1] = (object)obj;
						object result = mi.Invoke(typeFound,
							System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public |
							System.Reflection.BindingFlags.NonPublic, null, parameters, null);
						if (!(bool)result)
							return NodeTestResult.kntrNothing;
					}
				}
				else if (flid == 0 && editor != null && editor == "autocustom")
				{
					flid = SliceFactory.GetCustomFieldFlid(caller, m_cache.MetaDataCacheAccessor, obj);
				}

				if (flid != 0)
				{
					FieldType fieldType = m_cache.GetFieldType(flid);
					fieldType &= FieldType.kcptVirtualMask; // strip virtual bit.
					switch (fieldType)
					{
						default: // if we don't know how to check, make it visible.
							break;
							// These cases are a bit tricky. We're duplicating some information here about how the slices
							// interpret their ws parameter. Don't see how to avoid it, though, without creating the slices even if not needed.
						case FieldType.kcptMultiString:
						case FieldType.kcptMultiUnicode:
						case FieldType.kcptMultiBigString:
						case FieldType.kcptMultiBigUnicode:
							string ws = XmlUtils.GetOptionalAttributeValue(node, "ws", null);
							switch(ws)
							{
								case "vernacular":
									if (m_cache.MainCacheAccessor.get_MultiStringAlt(obj.Hvo, flid, m_cache.DefaultVernWs).Length == 0)
										return NodeTestResult.kntrNothing;
									break;
								case "analysis":
									if (m_cache.MainCacheAccessor.get_MultiStringAlt(obj.Hvo, flid, m_cache.DefaultAnalWs).Length == 0)
										return NodeTestResult.kntrNothing;
									break;
								default:
									if (editor == "jtview")
									{
										if (m_cache.MainCacheAccessor.get_MultiStringAlt(obj.Hvo, flid, m_cache.DefaultAnalWs).Length == 0)
											return NodeTestResult.kntrNothing;
									}
									// try one of the magic ones for multistring
									int wsMagic = LangProject.GetMagicWsIdFromName(ws);
									if (wsMagic == 0 && editor == "autocustom")
									{
										wsMagic = m_cache.MetaDataCacheAccessor.GetFieldWs((uint)flid);
									}
									if (wsMagic == 0 && editor != "autocustom")
										break; // not recognized, treat as visible
									ILgWritingSystem[] rgws = SIL.FieldWorks.Common.Widgets.LabeledMultiStringView.GetWritingSystemList(m_cache, wsMagic, false);
									bool anyNonEmpty = false;
									foreach (ILgWritingSystem wsInst in rgws)
									{
										if (m_cache.MainCacheAccessor.get_MultiStringAlt(obj.Hvo, flid, wsInst.Hvo).Length != 0)
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
						case FieldType.kcptString:
						case FieldType.kcptBigString:
							if (m_cache.MainCacheAccessor.get_StringProp(obj.Hvo, flid).Length == 0)
								return NodeTestResult.kntrNothing;
							break;
						case FieldType.kcptUnicode:
						case FieldType.kcptBigUnicode:
							string val = m_cache.MainCacheAccessor.get_UnicodeProp(obj.Hvo, flid);
							if (val == null || val.Length == 0)
								return NodeTestResult.kntrNothing;
							break;
							// Usually, the header nodes for sequences and atomic object props
							// have no editor. But sometimes they may have a jtview summary
							// or the like. If an object-prop flid is specified, check it,
							// in case we want to suppress the whole header.
						case FieldType.kcptOwningAtom:
						case FieldType.kcptReferenceAtom:
							int hvoT = m_cache.MainCacheAccessor.get_ObjectProp(obj.Hvo, flid);
							if (hvoT == 0)
								return NodeTestResult.kntrNothing;
							int clid = m_cache.GetClassOfObject(hvoT);
							if (clid == (int)CellarModuleDefns.kclidStText) // if clid is an sttext clid
							{
								// Test if the StText has only one paragraph
								int cpara = m_cache.GetVectorSize(hvoT, (int)CellarModuleDefns.kflidStText_Paragraphs);
								if (cpara == 1)
								{
									// Tests if paragraph is empty
									int hvoPara = m_cache.GetVectorItem(hvoT, (int)CellarModuleDefns.kflidStText_Paragraphs, 0);
									if (hvoPara == 0)
										return NodeTestResult.kntrNothing;
									ITsString tss = m_cache.GetTsStringProperty(hvoPara, (int)CellarModuleDefns.kflidStTxtPara_Contents);
									if (tss == null || tss.Length == 0)
										return NodeTestResult.kntrNothing;
								}
							}
							break;
						case FieldType.kcptOwningCollection:
						case FieldType.kcptOwningSequence:
						case FieldType.kcptReferenceCollection:
						case FieldType.kcptReferenceSequence:
							if (m_cache.MainCacheAccessor.get_VecSize(obj.Hvo, flid) == 0)
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
						if (node.GetType() == typeof(XmlComment))
							continue;
						cnodes++;
						if (cnodes > 1)
							break;
						child = n;
					}
					if (cnodes == 1) // exactly one non-comment child
					{
						int flidChild = GetFlidFromNode(child, obj);
						// If it's an obj or seq node and the property is empty, we'll show nothing.
						if (flidChild != 0 && child.Name == "seq" &&
							m_cache.MainCacheAccessor.get_VecSize(obj.Hvo, flidChild) == 0)
						{
							return NodeTestResult.kntrNothing;
						}
						if (flidChild != 0 && child.Name == "obj" &&
							m_cache.MainCacheAccessor.get_ObjectProp(obj.Hvo, flidChild) == 0)
						{
							return NodeTestResult.kntrNothing;
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
				slice = SliceFactory.Create(m_cache, editor, flid, node, obj, StringTbl, PersistenceProvder, m_mediator, caller);
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
				slice.Mediator = m_mediator;


				// We need a copy since we continue to modify path, so make it as compact as possible.
				slice.Key = path.ToArray();
				slice.ConfigurationNode = node;
				slice.CallerNode = caller;
				slice.OverrideBackColor(XmlUtils.GetOptionalAttributeValue(node, "backColor"));
				slice.ShowContextMenu += new TreeNodeEventHandler(this.OnShowContextMenu);
				slice.SmallImages = SmallImages;
				SetNodeWeight(node, slice);

				slice.FinishInit();
				slice.Visible = false; // don't show it until we position and size it.

				InsertSliceAndRegisterWithContextHelp(insPos, slice, node);
			}
			else
			{
				EnsureValidIndexForReusedSlice(slice, insPos);
			}
			insPos++;
			slice.GenerateChildren(node, caller, obj, indent, ref insPos, path, reuseMap);
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
				label = XmlUtils.GetLocalizedAttributeValue(Mediator.StringTbl, caller, attr, null);
				if (label == null)
					label = XmlUtils.GetLocalizedAttributeValue(Mediator.StringTbl, node, attr, null);
			}
			else
			{
				label = XmlUtils.GetOptionalAttributeValue(caller, attr);
				if (label == null)
					label = XmlUtils.GetOptionalAttributeValue(node, attr);
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
			if (label != null && this.StringTbl != null)
			{
				abbr = this.StringTbl.GetString(label, "LabelAbbreviations");
				if (abbr == "*" + label + "*")
					abbr = null;	// couldn't find it in the StringTable, reset it to null.
			}
			abbr = InterpretLabelAttribute(abbr, obj);
			// NOTE: Currently, Slice.Abbreviation Property sets itself to a 4-char truncation of Slice.Label
			// internally when setting the property to null.  So, allow abbr == null, and let that code handle
			// the truncation.
			return abbr;
		}

		private void SetNodeWeight(XmlNode node, Slice slice)
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

		private void RegisterWithContextHelp(Slice slice, XmlNode configuration)
		{
			slice.RegisterWithContextHelper();
		}

		/// <summary>
		/// Get the context menu that would be displayed for a right click on the slice.
		/// </summary>
		/// <param name="slice"></param>
		/// <returns></returns>
		public ContextMenu GetSliceContextMenu(Slice slice, bool fHotLinkOnly)
		{
			CheckDisposed();
			Debug.Assert(ShowContextMenuEvent!= null, "this should always be set to something");
			// This is something of a historical artifact. There's probably no reason
			// to pass a point to ShowContextMenuEvent. At an earlier stage, the event was
			// ShowContextMenu, so it needed a point. TreeNodeEventArgs is still used for
			// Slice.ShowContextMenu event, so it was somewhat awkward to change.
			SliceMenuRequestArgs e = new SliceMenuRequestArgs(slice, fHotLinkOnly);
			return ShowContextMenuEvent(this, e);
		}

		/// <summary>
		/// this is called by a client which normally provides its own custom menu, in order to allow it to
		/// fall back on an auto menu during development, before the custom menu has been defined.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
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
		/// <param name="obj"></param>
		/// <param name="template"></param>
		/// <param name="indent"></param>
		/// <param name="insertPosition"></param>
		/// <param name="path"></param>
		/// <param name="reuseMap"></param>
		/// <returns></returns>
		public int ApplyChildren(ICmObject obj, XmlNode template, int indent, int insertPosition, ArrayList path, ObjSeqHashMap reuseMap)
		{
			CheckDisposed();
			int insertPos = insertPosition;
			foreach (XmlNode node in template.ChildNodes)
			{
				if (node.Name == "ChangeRecordHandler")
					continue;	// Handle only at the top level (at least for now).
				insertPos = ApplyLayout(obj, node, indent, insertPos, path, reuseMap);
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
				if (slice.BecomeRealInPlace())
				{
					SetTabIndex(Controls.IndexOf(slice));
					return slice;
				}
				AboutToCreateField();
				slice.BecomeReal(i);
				RemoveSliceAt(i);
				if (i >= Controls.Count)
				{
					// BecomeReal produced nothing; range has decreased!
					return null;
				}
				// Make sure something changed; otherwise, we have an infinite loop here.
				Debug.Assert(slice != Controls[i]);
				slice = (Slice)Controls[i];
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

			Slice slice = Controls[i] as Slice;
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
				int hvoCurrentObj = 0;
				if (m_currentSlice != null)
				{
					if (m_currentSlice.ConfigurationNode != null &&
						m_currentSlice.ConfigurationNode.ParentNode != null)
					{
						sCurrentPartName = XmlUtils.GetAttributeValue(m_currentSlice.ConfigurationNode.ParentNode,
							"id", String.Empty);
					}
					if (m_currentSlice.Object != null)
						hvoCurrentObj = m_currentSlice.Object.Hvo;
				}
				SetCurrentSlicePropertyNames();
				m_mediator.PropertyTable.SetProperty(m_sPartNameProperty, sCurrentPartName, false, PropertyTable.SettingsGroup.LocalSettings);
				m_mediator.PropertyTable.SetProperty(m_sObjHvoProperty, hvoCurrentObj, false, PropertyTable.SettingsGroup.LocalSettings);
				m_mediator.PropertyTable.SetPropertyPersistence(m_sPartNameProperty, true, PropertyTable.SettingsGroup.LocalSettings);
				m_mediator.PropertyTable.SetPropertyPersistence(m_sObjHvoProperty, true, PropertyTable.SettingsGroup.LocalSettings);
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
			if (m_layoutState == LayoutStates.klsLayoutSuspended || m_layoutState == LayoutStates.klsClearingAll)
				return;
			bool fNeedInternalLayout = true; // call HandleLayout1 at least once

			Size smallestSize = new Size();
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
					AutoScrollPosition = new Point(-aspOld.X, -aspOld.Y);

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
				int yTop = HandleLayout1(true, clipRect);
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
		/// <param name="clipRect"></param>
		/// <returns></returns>
		protected internal int HandleLayout1(bool fFull, Rectangle clipRect)
		{
			if (m_fDisposing)
				return clipRect.Bottom; // don't want to lay out while clearing slices in dispose!

			int minHeight = GetMinFieldHeight();
			int desiredWidth = this.ClientRectangle.Width;
			int yTop = AutoScrollPosition.Y;
			for (int i = 0; i < Controls.Count; i++)
			{
				// Don't care about items below bottom of clip, if one is specified.
				if ((!fFull) && yTop >= clipRect.Bottom)
				{
					return yTop - AutoScrollPosition.Y; // not very meaningful in this case, but a result is required.
				}
				Slice tci = Controls[i] as Slice;
				// Best guess of its height, before we ensure it's real.
				int defHeight = tci == null ? minHeight : tci.Height;
				bool fSliceIsVisible = !fFull && yTop + defHeight > clipRect.Top && yTop <= clipRect.Bottom;

				//Debug.WriteLine(String.Format("DataTree.HandleLayout1({3},{4}): fSliceIsVisible = {5}, i = {0}, defHeight = {1}, yTop = {2}, desiredWidth = {7}, tci.Config = {6}",
				//    i, defHeight, yTop, fFull, clipRect.ToString(), fSliceIsVisible, tci.ConfigurationNode.OuterXml, desiredWidth));

				if (fSliceIsVisible)
				{
					// We cannot allow slice to be unreal; it's visible, and we're checking
					// for real slices where they're visible
					Point oldPos = AutoScrollPosition;
					tci = FieldAt(i); // ensures it becomes real if needed.
					// In the course of becoming real it may have changed height (more strictly, its
					// real height may be different from the previous estimated height).
					// If it was previously above the top of the window, this can produce an unwanted
					// change in the visble position of previously visible slices.
					// The scroll position may also have changed as a result of the blankety blank
					// blank undocumented behavior of the UserControl class trying to make what it
					// thinks is the interesting child control visible.
					Point desiredScrollPosition = new Point(-oldPos.X, -oldPos.Y);
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
					if (-AutoScrollPosition.Y != desiredScrollPosition.Y)
						AutoScrollPosition = desiredScrollPosition;
				}
				if (tci == null)
				{
					yTop += minHeight;
				}
				else
				{
					// Move this slice down a little if it needs a heavy rule above it
					if (tci.Weight == ObjectWeight.heavy)
						yTop += DataTree.HeavyweightRuleThickness + DataTree.HeavyweightRuleAboveMargin;
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
			return yTop - AutoScrollPosition.Y;
		}

		private void MakeSliceRealAt(int i)
		{
			// We cannot allow slice to be unreal; it's visible, and we're checking
			// for real slices where they're visible
			Point oldPos = AutoScrollPosition;
			Slice tci = Controls[i] as Slice;
			int oldHeight = tci == null ? GetMinFieldHeight() : tci.Height;
			tci = FieldAt(i); // ensures it becomes real if needed.
			int desiredWidth = this.ClientRectangle.Width;
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
			Point desiredScrollPosition = new Point(-oldPos.X, -oldPos.Y);
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
					Control ctrl = tci.ContainingDataTree.Controls[i];
					if (ctrl != null && !ctrl.Visible)
						ctrl.Visible = true;
				}
				tci.Visible = true;
				Debug.Assert(tci.IndexInContainer == index,
					String.Format("MakeSliceVisible: slice '{0}' at index({2}) should not have changed to index ({1})." +
					" This can occur when making slices visible in an order different than their order in DataTree.Controls. See LT-7307.",
					(tci.ConfigurationNode != null && tci.ConfigurationNode.OuterXml != null ? tci.ConfigurationNode.OuterXml : "(DummySlice?)"),
				tci.IndexInContainer, index));
				// This was moved out of the Control setter because it prematurely creates
				// root boxes (because it creates a window handle). The embedded control shouldn't
				// need an accessibility name before it is visible!
				if (tci.Label != null && tci.Label.Length > 0 && tci.Control != null)
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
		public int NextFieldAtIndent(int nInd, int iStart)
		{
			CheckDisposed();
			int cItem = Controls.Count;

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
		/// <param name="index"></param>
		/// <returns></returns>
		int HeightOfSliceOrNullAt(int iSlice)
		{
			Slice tc = Controls[iSlice] as Slice;
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
			for (int iSlice = 0; iSlice < Controls.Count; iSlice++)
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
					HandlePaintLinesBetweenSlices(null, e);
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

				base.ActiveControl = value;
				foreach (Slice slice in Controls)
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
				if (Controls.Count == 0)
					return null;
				return (Slice)Controls[Controls.Count - 1];
			}
		}

		/// <summary>
		/// Moves the focus to the next visible slice in the tree
		/// </summary>
		public void GotoNextSlice()
		{
			CheckDisposed();

			if (m_currentSlice != null)
				GotoNextSliceAfterIndex(Controls.IndexOf(m_currentSlice));
		}

		internal bool GotoNextSliceAfterIndex(int index)
		{
			CheckDisposed();
			++index;
			while (index >= 0 && index < Controls.Count)
			{
				Slice current = FieldAt(index);
				DataTree.MakeSliceVisible(current);
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
			while (index >= 0 && index < Controls.Count)
			{
				Slice current = FieldAt(index);
				DataTree.MakeSliceVisible(current);
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
				else
					return new IxCoreColleague[] {this};
			}
			else
			{
				// If we're not visible, we don't want to be a message target.
				// It is remotely possible that the current slice still does, though.
				if (m_currentSlice != null)
					return new IxCoreColleague[] { m_currentSlice };
				else
					return new IxCoreColleague[0];
			}
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
			int hvo = GetHvoForJumpToTool((Command)commandObject, out tool);
			if (hvo != 0)
			{
				display.Enabled = display.Visible = true;
				return true;
			}
			else
			{
				return false;
			}
		}

		/// <summary>
		/// Handle enabled menu items for jumping to the concordance (or lexiconEdit) tool.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <returns></returns>
		public virtual bool OnJumpToTool(object commandObject)
		{
			CheckDisposed();
			string tool;
			int hvo = GetHvoForJumpToTool((Command)commandObject, out tool);
			if (hvo != 0)
			{
				m_mediator.PostMessage("FollowLink",
					SIL.FieldWorks.FdoUi.FwLink.Create(tool, m_cache.GetGuidFromId(hvo), m_cache.ServerName, m_cache.DatabaseName));
				return true;
			}
			else
			{
				return false;
			}
		}

		/// <summary>
		/// Common logic shared between OnDisplayJumpToTool and OnJumpToTool.
		/// </summary>
		/// <param name="cmd"></param>
		/// <param name="tool"></param>
		/// <returns></returns>
		private int GetHvoForJumpToTool(Command cmd, out string tool)
		{
			tool = SIL.Utils.XmlUtils.GetManditoryAttributeValue(cmd.Parameters[0], "tool");
			string className = SIL.Utils.XmlUtils.GetManditoryAttributeValue(cmd.Parameters[0], "className");
			if (CurrentSlice != null && CurrentSlice.Object != null)
			{
				if (tool == "concordance")
				{
					int flidSlice = 0;
					if (!CurrentSlice.IsHeaderNode)
					{
						flidSlice = CurrentSlice.Flid;
						if (flidSlice == 0 || m_mdc.get_IsVirtual((uint)flidSlice))
							return 0;
					}
					switch (className)
					{
						case "LexEntry":
							if (m_root != null && m_root.ClassID == LexEntry.kclsidLexEntry)
							{
								if (cmd.Id == "CmdRootEntryJumpToConcordance")
								{
									return m_root.Hvo;
								}
								else
								{
									if (CurrentSlice.Object.ClassID == LexEntryRef.kclsidLexEntryRef)
										return 0;	// handled elsewhere for this slice.
									int hvoCurrent = CurrentSlice.Object.Hvo;
									if (CurrentSlice.Object.ClassID == LexEntry.kclsidLexEntry)
										return hvoCurrent;
									else
										return m_cache.GetOwnerOfObjectOfClass(hvoCurrent, LexEntry.kclsidLexEntry);
								}
							}
							break;
						case "LexSense":
							if (CurrentSlice.Object.ClassID == LexSense.kclsidLexSense)
							{
								int hvoEntry = m_cache.GetOwnerOfObjectOfClass(CurrentSlice.Object.Hvo,
									(int)LexEntry.kclsidLexEntry);
								if (hvoEntry == m_root.Hvo)
									return CurrentSlice.Object.Hvo;
							}
							break;
						case "MoForm":
							if (m_cache.ClassIsOrInheritsFrom((uint)CurrentSlice.Object.ClassID, (uint)MoForm.kclsidMoForm))
							{
								if (flidSlice == (int)MoForm.MoFormTags.kflidForm)
									return CurrentSlice.Object.Hvo;
							}
							break;
					}
				}
				else if (tool == "lexiconEdit")
				{
					if (CurrentSlice.Object.OwnerHVO != 0 &&
						CurrentSlice.Object.OwnerHVO != m_root.Hvo &&
						m_cache.GetClassOfObject(CurrentSlice.Object.OwnerHVO) == (int)LexEntry.kclsidLexEntry)
					{
						return CurrentSlice.Object.OwnerHVO;
					}
				}
			}
			return 0;
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
		/// (LT-8564).
		/// </summary>
		/// <param name="arg"></param>
		public void OnFocusFirstPossibleSlice(object arg)
		{
			Application.Idle += new EventHandler(DoPostponedFocusSlice);
		}

		void DoPostponedFocusSlice(object sender, EventArgs e)
		{
			Application.Idle -= new EventHandler(DoPostponedFocusSlice);
			// If the user switches tools quickly after inserting an object, the new view may
			// already be created before this gets called to set the focus in the old view.
			// Therefore we don't want to crash, we just want to do nothing.  See LT-8698.
			if (IsDisposed)
				return;
			if (CurrentSlice == null)
				FocusFirstPossibleSlice();
		}

		private void HandleShowHiddenFields(bool newShowValue)
		{
			if (newShowValue != m_fShowAllFields)
			{
				m_fShowAllFields = newShowValue;
				RefreshList(true);
			}
		}

		/// <summary>
		/// Invoked by a slice when the user does something to bring up a context menu
		/// </summary>
		public void OnShowContextMenu(object sender, TreeNodeEventArgs e)
		{
			CheckDisposed();
			//just pass this onto, for example, the XWorks View that owns us,
			//assuming that it has subscribed to this event on this object.
			//If it has not, then this will still point to the "auto menu handler"
			Debug.Assert(ShowContextMenuEvent != null, "this should always be set to something");
			CurrentSlice = e.Slice;
			SliceMenuRequestArgs args = new SliceMenuRequestArgs(e.Slice, false);
			ShowContextMenuEvent(sender, args);
			//			ContextMenu menu = ShowContextMenuEvent(sender, args);
			//			menu.Show(e.Context, e.Location);
		}

		/// <summary>
		/// Process the message to allow setting/focusing CurrentSlice.
		/// </summary>
		/// <param name="sender"></param>
		public void OnReadyToSetCurrentSlice(object sender)
		{
			CheckDisposed();
			// we should now be ready to put our focus in a slice.
			m_fSuspendSettingCurrentSlice = false;
			try
			{
				SetDefaultCurrentSlice();
			}
			finally
			{
				m_fCurrentContentControlObjectTriggered = false;
			}
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
		protected virtual void SetDefaultCurrentSlice()
		{
			Slice sliceToSetAsCurrent = m_currentSliceNew;
			m_currentSliceNew = null;
			if (sliceToSetAsCurrent != null && sliceToSetAsCurrent.IsDisposed)
				sliceToSetAsCurrent = null;	// someone's creating slices faster than we can display!
			// try to see if any of our current slices have focus. if so, use that one.
			if (sliceToSetAsCurrent == null)
			{
				Control focusedControl = XWindow.FocusedControl();
				if (this.ContainsFocus)
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
				if (!m_currentSlice.Focused)	// probably coming from m_currentSliceNew
				{
					ScrollControlIntoView(m_currentSlice);
					// For string type slices, place cursor at end of (top) line.  This works
					// more reliably than putting it at the beginning for some reason, and makes
					// more sense in some circumstances (especially in the conversion from a ghost
					// slice to a string type slice).
					if (m_currentSlice is MultiStringSlice)
					{
						MultiStringSlice mss = m_currentSlice as MultiStringSlice;
						mss.SelectAt(mss.WritingSystemOptionsForDisplay[0].Hvo, 99999);
					}
					else if (m_currentSlice is StringSlice)
					{
						(m_currentSlice as StringSlice).SelectAt(99999);
					}
					m_currentSlice.TakeFocus(false);
				}
			}
			// otherwise, try to select the first slice, if it won't conflict with
			// an existing cursor (cf. LT-8211), like when we're first starting up/switching tools
			// as indicated by m_fCurrentContentControlObjectTriggered.
			if (CurrentSlice == null && m_fCurrentContentControlObjectTriggered)
				FocusFirstPossibleSlice();
		}

		/// <summary>
		/// Focus the first slice that can take focus.
		/// </summary>
		/// <returns></returns>
		protected Slice FocusFirstPossibleSlice()
		{
			int cslice = Controls.Count;
			for (int islice = 0; islice < cslice; ++islice)
			{
				Slice slice = Controls[islice] as Slice;
				if (slice.TakeFocus(false))
					return slice;
			}
			return null;
		}

		#endregion IxCoreColleague message handlers

		/// <summary>
		/// Influence the display of a particular command by giving an opinion on whether we
		/// are prepared to handle the corresponding "InsertItemViaBackrefVector" message.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public bool OnDisplayInsertItemViaBackrefVector(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			// We may be in transition: if so, disable without crashing.  See LT-9698.
			if (m_cache == null || m_root == null)
				return display.Enabled = false;
			Command command = (Command)commandObject;
			string className = XmlUtils.GetManditoryAttributeValue(command.Parameters[0], "className");
			if (className != m_cache.GetClassName((uint)m_root.ClassID))
				return display.Enabled = false;
			string restrictToTool = XmlUtils.GetOptionalAttributeValue(command.Parameters[0], "restrictToTool");
			if (restrictToTool != null && restrictToTool != m_mediator.PropertyTable.GetStringProperty("currentContentControl", String.Empty))
				return display.Enabled = false;
			string fieldName = XmlUtils.GetOptionalAttributeValue(command.Parameters[0], "fieldName");
			if (String.IsNullOrEmpty(fieldName) || m_cache.GetVirtualProperty(className, fieldName) == null)
				return display.Enabled = false;

			return display.Enabled = true;
		}

		/// <summary>
		/// This is triggered by any command whose message attribute is "InsertItemViaBackrefVector"
		/// </summary>
		/// <param name="argument"></param>
		/// <returns>true if successful (the class is known)</returns>
		public bool OnInsertItemViaBackrefVector(object argument)
		{
			CheckDisposed();

			Command command = (Command)argument;
			string className = XmlUtils.GetManditoryAttributeValue(command.Parameters[0], "className");
			if (className != m_cache.GetClassName((uint)m_root.ClassID))
				return false;
			string restrictToTool = XmlUtils.GetOptionalAttributeValue(command.Parameters[0], "restrictToTool");
			if (restrictToTool != null && restrictToTool != m_mediator.PropertyTable.GetStringProperty("currentContentControl", String.Empty))
				return false;
			string fieldName = XmlUtils.GetOptionalAttributeValue(command.Parameters[0], "fieldName");
			if (String.IsNullOrEmpty(fieldName))
				return false;
			IVwVirtualHandler vh = m_cache.GetVirtualProperty(className, fieldName);
			int insertPos = Slice.InsertObjectIntoVirtualBackref(m_cache, m_mediator, vh, m_root.Hvo,
				(uint)m_root.ClassID, (uint)LexDb.kclsidLexDb, (uint)vh.Tag);
			return insertPos >= 0;
		}
	}

	class DummyObjectSlice : Slice
	{
		XmlNode m_node; // Node with name="seq" that controls the sequence we're a dummy for
		// Path of parent slice info up to and including m_node.
		// We can't use a List<int>, as the Arraylist may hold XmlNodes and ints, at least.
		ArrayList m_path;
		int m_flid; // sequence field we're a dummy for
		int m_ihvoMin; // index in sequence of first object we stand for.
		string m_layoutName;
		XmlNode m_caller; // Typically "partRef" node that invoked the part containing the <seq>

		/// <summary>
		/// Create a slice. Note that callers that will further modify path should pass a Clone.
		/// </summary>
		/// <param name="dt"></param>
		/// <param name="indent"></param>
		/// <param name="node"></param>
		/// <param name="path"></param>
		/// <param name="obj"></param>
		/// <param name="flid"></param>
		/// <param name="ihvoMin"></param>
		public DummyObjectSlice(DataTree dt, int indent, XmlNode node, ArrayList path, ICmObject obj, int flid,
			int ihvoMin, string layoutName, XmlNode caller)
		{
			m_indent = indent;
			m_node = node;
			m_path = path;
			m_obj = obj;
			m_flid = flid;
			m_ihvoMin = ihvoMin;
			m_layoutName = layoutName;
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
			m_caller = null;
			m_layoutName = null;
			m_node = null;
			m_path = null;

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
			for (int islice = index - 1; islice >= 0 && Parent.Controls[islice] == this; islice--)
				ihvo++;
			string mode = XmlUtils.GetOptionalAttributeValue(m_node, "mode");
			int hvo = m_cache.GetVectorItem(m_obj.Hvo, m_flid, ihvo);
			// In the course of becoming real, we may get disposed. That clears m_path, which
			// has various bad effects on called objects that are trying to use it, as well as
			// causing failure here when we try to remove the thing we added temporarily.
			// Work with a copy, so Dispose can't get at it.
			ArrayList path = new ArrayList(m_path);
			if (ihvo == m_ihvoMin)
			{
				// made the first element real. Increment start ihvo: the first thing we are a
				// dummy for got one greater
				m_ihvoMin++;
			}
			else if (index < Parent.Controls.Count && Parent.Controls[index + 1] == this)
			{
				// Any occurrences after index get replaced by a new one with suitable ihvoMin.
				// Note this must be done before we insert an unknown number of extra slices
				// by calling CreateSlicesFor.
				DummyObjectSlice dosRep = new DummyObjectSlice(ContainingDataTree, m_indent, m_node, path,
					m_obj, m_flid, ihvo + 1, m_layoutName, m_caller);
				dosRep.Cache = this.Cache;
				for (int islice = index + 1;
					islice < Parent.Controls.Count && Parent.Controls[islice] == this;
					islice++)
				{
					ContainingDataTree.RawSetSlice(islice, dosRep);
				}
			}

			// Save these, we may get disposed soon, can't get them from member data any more.
			DataTree containingTree = ContainingDataTree;
			Control parent = Parent;

			path.Add(hvo);
			ICmObject objItem = CmObject.CreateFromDBObject(ContainingDataTree.Cache, hvo);
			Point oldPos = ContainingDataTree.AutoScrollPosition;
			int insertPosition = ContainingDataTree.CreateSlicesFor(objItem, m_layoutName, m_indent, index + 1, path,
				new ObjSeqHashMap(), m_caller);
			// If inserting slices somehow altered the scroll position, for example as the
			// silly Panel tries to make the selected control visible, put it back!
			if (containingTree.AutoScrollPosition != oldPos)
				containingTree.AutoScrollPosition = new Point(-oldPos.X, -oldPos.Y);
			// No need to remove, we added to copy.
			//m_path.RemoveAt(m_path.Count - 1);
			if (parent.Controls.Count > index + 1)
				return parent.Controls[index + 1] as Slice;
			else
				return null;
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
