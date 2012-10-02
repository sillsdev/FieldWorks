using System;
using System.Windows.Forms;
using System.Drawing;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.IO;

using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.Controls;
using SIL.Utils;
using SIL.FieldWorks.Common.RootSites;
using XCore;
using SIL.FieldWorks.FdoUi;
using SIL.FieldWorks.LexText.Controls;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// A Slice is essentially one row of a tree.
	/// It contains both a SliceTreeNode on the left of the splitter line, and a
	/// (optional) subclass of control on the right.
	/// </summary>
	/// <remarks>Slices know about drawing labels, measuring height, dealing with drag operations
	/// within the tree for this item, knowing whether the item can be expanded,
	/// and optionally drawing the part of the tree that is opposite the item, and
	/// many other things.}
#if SLICE_IS_SPLITCONTAINER
	/// The problem I (RandyR) ran into with this is when the DataTree scrolled and reset the Top of the slice,
	/// the internal SplitterRectangle ended up being non-0 in many cases,
	/// which resulted in the splitter not be in the right place (visible)
	/// The MS docs say in a vertical orientation like this, the 'Y"
	/// value of SplitterRectangle will always be 0.
	/// I don't know if it is a bug in the MS code or in our code that lets it be non-0,
	/// but I worked with it quite a while without finding the true problem.
	/// So, I went back to a Slice having a SplitContainer,
	/// rather than the better option of it being a SplitContainer.
	///</remarks>
	public class Slice : SplitContainer, IxCoreColleague, IFWDisposable
#else
	///</remarks>
	public class Slice : UserControl, IxCoreColleague, IFWDisposable
#endif
	{
		#region Constants

		/// <summary>
		/// If label width is made wider than this, switch to full labels.
		/// </summary>
		const int MaxAbbrevWidth = 60;

		#endregion Constants

		#region Data members

		//subscribe to this event if you want to provide a Context menu for this slice
		public event TreeNodeEventHandler ShowContextMenu;

		// These two variables allow us to save the parameters passed in IxCoreColleage.Init
		// so we can pass them on when our control is set.
		private Mediator m_mediator;
		XmlNode m_configurationParameters;

		//test
		//		protected MenuController m_menuController= null;
		protected ImageCollection m_smallImages = null;
		//end test

		protected int m_indent = 0;
		protected DataTree.TreeItemState m_expansion = DataTree.TreeItemState.ktisFixed; // Default is not expandable.
		protected string m_strLabel;
		protected string m_strAbbr;
		protected bool m_isHighlighted = false;
		protected Font m_fontLabel = new Font("Arial", 10);
		protected XmlNode m_configurationNode; // If this slice was generated from an XmlNode, store it here.
		protected XmlNode m_callerNode;	// This stores the layout time caller for menu processing
		protected Point m_location;
		protected ICmObject m_obj; // The object that will be the context if our children are expanded, or for figuring
		// what things can be inserted here.
		protected object[] m_key; // Key indicates path of nodes and objects used to construct this.
		protected FdoCache m_cache;
		protected SIL.Utils.StringTable m_stringTable;
		// Indicates the 'weight' of object that starts at the top of this slice.
		// By default a slice is just considered to be a field (of the same object as the one before).
		protected ObjectWeight m_weight = ObjectWeight.field;
		protected bool m_widthHasBeenSetByDataTree = false;
		// trace switch used to limit Dispose traces when obj is dispos(ed/ing) and still getting msgs
		private TraceSwitch disposeSwitch = new TraceSwitch("SIL.DisposeTrace", "Dispose tracking", "Off");

		#endregion Data members

		#region Properties

		/// <summary>
		/// The weight of object that starts at the beginning of this slice.
		/// </summary>
		public ObjectWeight Weight
		{
			get
			{
				CheckDisposed();

				return m_weight;
			}
			set
			{
				CheckDisposed();

				m_weight = value;
			}
		}

		public ContextMenu RetrieveContextMenuForHotlinks()
		{
			CheckDisposed();

			return ContainingDataTree.GetSliceContextMenu(this, true);
		}

		public object[] Key
		{
			get
			{
				CheckDisposed();

				return m_key;
			}
			set
			{
				CheckDisposed();

				m_key = value;
			}
		}
		public virtual XCore.Mediator Mediator
		{
			get
			{
				CheckDisposed();

				return m_mediator;
			}
			set
			{
				CheckDisposed();

				m_mediator = value;
				SIL.FieldWorks.Common.RootSites.SimpleRootSite rs =
					this.Control as SIL.FieldWorks.Common.RootSites.SimpleRootSite;
				if (rs != null)
				{
					rs.Init(this.Mediator, m_configurationNode); // Init it as xCoreColleague.
				}
				else if (this.Control != null && this.Control.Controls != null)
				{
					// If not a SimpleRootSite, maybe it owns one. Init that as xCoreColleague.
					for (int i = 0; i < this.Control.Controls.Count; ++i)
					{
						rs = this.Control.Controls[i] as
							SIL.FieldWorks.Common.RootSites.SimpleRootSite;
						if (rs != null)
							rs.Init(this.Mediator, m_configurationNode);
					}
				}
			}
		}

		public DataTree ContainingDataTree
		{
			get
			{
				CheckDisposed();

				return Parent as DataTree;
			}
		}
		protected internal SplitContainer SplitCont
		{
			get
			{
				CheckDisposed();

#if SLICE_IS_SPLITCONTAINER
				return this;
#else
				return Controls[0] as SplitContainer;
#endif
			}
		}

		public SliceTreeNode TreeNode
		{
			get
			{
				CheckDisposed();

				return SplitCont.Panel1.Controls[0] as SliceTreeNode;
			}
		}

		public FDO.FdoCache Cache
		{
			get
			{
				CheckDisposed();

				return m_cache;
			}
			set
			{
				CheckDisposed();

				m_cache = value;
			}
		}

		public ICmObject Object
		{
			get
			{
				CheckDisposed();

				return m_obj;
			}
			set
			{
				CheckDisposed();

				m_obj = value;
			}
		}

		/// <summary>
		/// the XmlNode that was used to construct this slice
		/// </summary>
		public XmlNode ConfigurationNode
		{
			get
			{
				CheckDisposed();

				return m_configurationNode;
			}
			set
			{
				CheckDisposed();

				m_configurationNode = value;
			}
		}

		/// This node stores the caller for future processing
		/// </summary>
		public XmlNode CallerNode
		{
			get
			{
				CheckDisposed();

				return m_callerNode;
			}
			set
			{
				CheckDisposed();

				m_callerNode = value;
			}
		}

		// Review JohnT: or just make it public? Or make more delegation methods?
		public virtual Control Control
		{
			get
			{
				CheckDisposed();

				Debug.Assert(SplitCont.Panel2.Controls.Count == 0 || SplitCont.Panel2.Controls.Count == 1);

				return SplitCont.Panel2.Controls.Count == 1 ? SplitCont.Panel2.Controls[0] : null;
			}
			set
			{
				CheckDisposed();

				Debug.Assert(SplitCont.Panel2.Controls.Count == 0);

				if (value != null)
				{
					SplitCont.Panel2.Controls.Add(value);
					// mediator was set first; pass it to colleague.
					if (m_mediator != null && value is IxCoreColleague)
						(value as IxCoreColleague).Init(m_mediator, m_configurationParameters);
				}
			}
		}

		/// <summary>
		/// this tells whether the slice should be treated as a handle on the owned atomic object which it
		/// refers to, for the purposes of deletion, copy, etc. For example, the Pronunciation field of
		/// LexVariant owns a LexPronunciation, so the Pronunciation field should have this attribute set.
		/// </summary>
		/// <example>MoEndoCompound has a "linker" slice which actually just wraps the attributes of a
		/// MoForm that is owned in the linker attribute. so when the user opens the context menu on this slice
		/// he really wants to be operating on the linker, not be owning MoEndoCompound.
		/// Another example. LexVariant owns an atomic LexPronunciation in a Pronunciation field. If we set
		/// wrapsAtomic for the Pronunciation field, then this returns true, allowing an Insert Pronunciation
		/// menu to activate.</example>
		public bool WrapsAtomic
		{
			get
			{
				CheckDisposed();

				return XmlUtils.GetOptionalBooleanAttributeValue(m_configurationNode, "wrapsAtomic", false);
			}
		}

		/// <summary>
		/// is this node representing a property which is an (ordered) sequence?
		/// </summary>
		public bool IsSequenceNode
		{
			get
			{
				CheckDisposed();

				if (ConfigurationNode == null)
					return false;
				XmlNode node = ConfigurationNode.SelectSingleNode("seq");
				if (node == null)
					return false;

				string field = XmlUtils.GetOptionalAttributeValue(node, "field");
				if (field == null || field.Length == 0)
					return false;

				Debug.Assert(m_obj != null, "JH Made a false assumption!");
				uint flid = GetFlid(field);
				Debug.Assert(flid != 0); // current field should have ID!
				//at this point we are not even thinking about showing reference sequences in the DataTree
				//so I have not dealt with that
				return (GetFieldType(flid) == (int)FDO.FieldType.kcptOwningSequence);
			}
		}

		/// <summary>
		/// is this node representing a property which is an (unordered) collection?
		/// </summary>
		public bool IsCollectionNode
		{
			get
			{
				CheckDisposed();

				return ConfigurationNode.SelectSingleNode("seq") != null && !IsSequenceNode;
			}
		}

		/// <summary>
		/// is this node a header?
		/// </summary>
		public bool IsHeaderNode
		{
			get
			{
				CheckDisposed();

				return XmlUtils.GetOptionalAttributeValue(ConfigurationNode, "header") == "true";
			}
		}

		/// <summary>
		/// whether the label should be highlighted or not
		/// </summary>
		public bool Highlighted
		{
			set
			{
				CheckDisposed();

				bool current = m_isHighlighted;
				m_isHighlighted = value;
				// See LT-5415 for how to get here with TreeNode == null, possibly while this
				// slice is being disposed in the call to the base class Dispose method.

				// If TreeNode is null, then this object has been disposed.
				// Since we now throw an exception, in the CheckDisposed method if it is disposed,
				// there is now no reason to ask if it is null.
				if (current != m_isHighlighted)
					Refresh();
				//TreeNode.Refresh();
			}
		}

		public ImageCollection SmallImages
		{
			get
			{
				CheckDisposed();

				return null; //no tree icons
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
		public StringTable StringTbl
		{
			get
			{
				CheckDisposed();

				return m_stringTable;
			}
			set
			{
				CheckDisposed();

				m_stringTable = value;
			}
		}

		#endregion Properties

		#region Construction and initialization

		public Slice()
		{
#if SLICE_IS_SPLITCONTAINER
			TabStop = false;
#else
			// Create a SplitContainer to hold the two (or one control.
			SplitContainer sc = new SplitContainer();
			sc.Dock = DockStyle.Fill;
			sc.TabStop = false;
			sc.AccessibleName = "Slice.SplitContainer";
			Controls.Add(sc);
#endif
		}

		public Slice(System.Windows.Forms.Control ctrlT)
			: this()
		{
			Control = ctrlT;
#if _DEBUG
			Control.CheckForIllegalCrossThreadCalls = true;
#endif
		}

		/// <summary>
		/// This method should be called once the various properties of the slice have been set,
		/// particularly the Cache, Object, Key, and Spec. The slice may create its Control in
		/// this method, so don't assume it exists before this is called. It should be called
		/// before installing the slice.
		/// </summary>
		public virtual void FinishInit()
		{
			CheckDisposed();
		}

		protected override void OnEnter(EventArgs e)
		{
			CheckDisposed();


			base.OnEnter(e);

			ContainingDataTree.CurrentSlice = this;

			TakeFocus(false);
		}

		#endregion Construction and initialization

		#region Miscellaneous UI methods

		public virtual void RegisterWithContextHelper()
		{
			CheckDisposed();

			if (this.Control != null)//grouping nodes do not have a control
			{
				//It's OK to send null as an id
				if (m_mediator != null) // helpful for robustness and testing.
				{
					StringTable tbl = null;
					if (m_mediator.HasStringTable)
						tbl = m_mediator.StringTbl;
					string caption = XmlUtils.GetLocalizedAttributeValue(tbl,
						ConfigurationNode, "label", "");
					m_mediator.SendMessage("RegisterHelpTargetWithId",
						new object[] { this.Control, caption, HelpId }, false);
				}
			}
		}

		protected virtual string HelpId
		{
			get
			{
				CheckDisposed();

				string id = XmlUtils.GetAttributeValue(ConfigurationNode, "id");

				//if the idea has not been added, try using the "field" attribute as the key
				if (null == id)
					id = XmlUtils.GetAttributeValue(ConfigurationNode, "field");

				return id;
			}
		}

		/// <summary>
		/// This is passed the color that the XDE specified, if any, otherwise null.
		/// The default is to use the normal window color for editable text.
		/// Subclasses which know they should have a different default should
		/// override this method, but normally should use the specified color if not
		/// null.
		/// </summary>
		/// <param name="clr"></param>
		public virtual void OverrideBackColor(String backColorName)
		{
			CheckDisposed();

			if (Control == null)
				return;

			if (backColorName != null)
			{
				if (backColorName == "Control")
					Control.BackColor = Color.FromKnownColor(KnownColor.ControlLight);
				else
					Control.BackColor = Color.FromName(backColorName);
			}
			else
				Control.BackColor = System.Drawing.SystemColors.Window;
		}

		/// <summary>
		/// We tend to get a visual stuttering effect if sub-controls are made visible before the
		/// main slice is correctly positioned. This method is called after the slice is positioned
		/// to give it a chance to make embedded controls visible.
		/// This default implementation does nothing.
		/// </summary>
		public virtual void ShowSubControls()
		{
			CheckDisposed();
		}

		#endregion Miscellaneous UI methods

		#region events, clicking, etc.

		public void OnTreeNodeClick(object sender, EventArgs args)
		{
			CheckDisposed();

			TakeFocus();
		}

		/// <summary>
		/// </summary>
		public void TakeFocus()
		{
			CheckDisposed();

			TakeFocus(true);
		}

		/// <summary>
		/// The slice should become the focus slice (and return true).
		/// If the fOkToFocusTreeNode argument is false, this should happen iff it has a control which
		/// is appropriate to focus.
		/// Note: JohnT: recently I noticed that trying to focus the tree node doesn't seem to do
		/// anything; I'm not sure passing true is useful.
		/// </summary>
		/// <param name="fOkToFocusTreeNode"></param>
		/// <returns></returns>
		public bool TakeFocus(bool fOkToFocusTreeNode)
		{
			CheckDisposed();

			Control ctrl = Control;
			if (ctrl != null && ctrl.CanFocus && ctrl.TabStop)
			{
				ctrl.Focus();
			}
			else if (fOkToFocusTreeNode)
			{
				TreeNode.Focus();
			}
			else
				return false;

			//this is a bit of a hack, because focus and OnEnter are related but not equivalent...
			//some slices  never get an on enter, but  claim to be focus-able.
			if (ContainingDataTree.CurrentSlice != this)
				ContainingDataTree.CurrentSlice = this;
			return true;
		}

		/// <summary>
		/// Focus the main child control, if possible.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnGotFocus(EventArgs e)
		{
			CheckDisposed();
			if (Disposing)
				return;
			DataTree.MakeSliceVisible(this); // otherwise no change our control can take focus.
			base.OnGotFocus(e);
			if (Control != null && Control.CanFocus)
				Control.Focus();
		}

		#endregion events, clicking, etc.

		#region Tree management

		/// <summary>
		/// In some contexts we insert into the slice array a 'dummy' slice
		/// which can handle some queries directly (e.g., it may know
		/// its indentation level) but needs to 'BecomeReal' if it becomes
		/// fully visible. The purpose is laziness...often we insert the
		/// same dummy slice into many locations, and they are progressively
		/// replaced with real ones.
		/// </summary>
		/// <returns></returns>
		public virtual bool IsRealSlice
		{
			get
			{
				CheckDisposed();

				return true;
			}
		}

		/// <summary>
		/// In some contexts, we use a "ghost" slice to represent data that
		/// has not yet been created.  These are "real" slices, but they don't
		/// represent "real" data.  Thus, for example, the underlying object
		/// can't be deleted because it doesn't exist.  (But the ghost slice
		/// may claim to have an object, because it needs such information to
		/// create the data once the user decides to type something...
		/// </summary>
		/// <returns></returns>
		public virtual bool IsGhostSlice
		{
			get
			{
				CheckDisposed();

				return false;
			}
		}

		/// <summary>
		/// Some 'unreal' slices can become 'real' (ready to actually display) without
		/// actually replacing themselves with a different object. Such slices override
		/// this method to do whatever is needed and then answer true. If a slice
		/// answers false to IsRealSlice, this is tried, and if it returns false,
		/// then BecomeReal is called.
		/// </summary>
		/// <returns></returns>
		public virtual bool BecomeRealInPlace()
		{
			CheckDisposed();

			return false;
		}

		/// <summary>
		/// In some contexts we insert into the slice array
		/// </summary>
		/// <returns></returns>
		public virtual Slice BecomeReal(int index)
		{
			CheckDisposed();

			return this;
		}

		private void SetViewStylesheet(Control control, DataTree tc)
		{
			SimpleRootSite rootSite = control as SimpleRootSite;
			if (rootSite != null && rootSite.StyleSheet == null)
				rootSite.StyleSheet = tc.StyleSheet;
			foreach (Control c in control.Controls)
				SetViewStylesheet(c, tc);
		}

		public virtual bool ShowContextMenuIconInTreeNode()
		{
			CheckDisposed();

			return this == ContainingDataTree.CurrentSlice;
		}

		public virtual void SetCurrentState(bool isCurrent)
		{
			CheckDisposed();

			if (Control != null && Control is INotifyControlInCurrentSlice)
				(Control as INotifyControlInCurrentSlice).SliceIsCurrent = isCurrent;
			if (TreeNode != null)
				TreeNode.Invalidate();
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="parent"></param>
		public virtual void Install(DataTree parent)
		{
			CheckDisposed();

			if (parent == null) // Parent == null ||
				throw new InvalidOperationException("The slice '" + GetType().Name + "' must be placed in the Parent.Controls property before installing it.");

			SplitContainer sc = SplitCont;
			if (sc.Panel1.Controls.Count > 0)
				throw new InvalidOperationException("The slice '" + GetType().Name + "' has already been installed, and has its TreeNode.");

			// removing the NAMELESS controls of the new 'SplitContainer'
			if (sc.Panel1.AccessibleName == null)
				sc.Panel1.AccessibleName = "Panel1";
			if (sc.Panel2.AccessibleName == null)
				sc.Panel2.AccessibleName = "Panel2";

			// Make a standard SliceTreeNode now.
			SliceTreeNode treeNode = new SliceTreeNode(this);
			treeNode.SuspendLayout();
			treeNode.Dock = DockStyle.Fill;
			sc.Panel1.Controls.Add(treeNode);
			sc.AccessibilityObject.Name = "SplitContainer";

			if (Label != null && Label.Length != 0)
			{
				// Susanna wanted to try five, rather than the default of four
				// to see if wider and still invisble made it easier to work with.
				// It may end up being made visible in a light grey color, but then it would
				// go back to the default of four.
				// Being visible at four may be too overpowering, so we may have to
				// manually draw a thin line to give the user a que as to where the splitter bar is.
				// Then, if it gets to be visible, we will probably need to add a bit of padding between
				// the line and the main slice content, or its text will be connected to the line.
				sc.SplitterWidth = 5;

				// It was hard-coded to 40, but it isn't right for indented slices,
				// as they then can be shrunk so narrow as to completely cover up their label.
				sc.Panel1MinSize = (20 * (Indent + 1)) + 20;
				sc.Panel2MinSize = 0; // min size of right pane
				// This makes the splitter essentially invisible.
				sc.BackColor = Color.FromKnownColor(KnownColor.Window); //to make it invisible
				treeNode.MouseEnter += new EventHandler(treeNode_MouseEnter);
				treeNode.MouseLeave += new EventHandler(treeNode_MouseLeave);
				treeNode.MouseHover += new EventHandler(treeNode_MouseEnter);
			}
			else
			{
				// SummarySlice is one of these kinds of Slices.
				//Debug.WriteLine("Slice gets no usable splitter: " + GetType().Name);
				sc.SplitterWidth = 1;
				sc.Panel1MinSize = LabelIndent();
				sc.SplitterDistance = LabelIndent();
				sc.IsSplitterFixed = true;
			}

			int newHeight = Height;
			Control mainControl = Control;
			if (mainControl != null)
			{
				// Has SliceTreeNode and Control.

				// Set stylesheet on every view-based child control that doesn't already have one.
				SetViewStylesheet(mainControl, parent);
				if (this.Label != null && this.Label.Length > 0)
					mainControl.AccessibilityObject.Name = this.Label;// + "ZZZ_Slice";
				else
					mainControl.AccessibilityObject.Name = "Slice_unknown";
				// By default the height of the slice comes from the height of the embedded
				// control.
				// Just store the new height for now, as actually settig it, will cause events,
				// and the slice has no parent yet, which will be bad for those event handlers.
				//this.Height = Math.Max(Control.Height, LabelHeight);
				newHeight = Math.Max(mainControl.Height, LabelHeight);
				mainControl.Dock = DockStyle.Fill;
				sc.FixedPanel = FixedPanel.Panel1;
			}
			else
			{
				// Has SliceTreeNode but no Control.

				// LexReferenceMultiSlice has no control, as of 12/30/2006.
				newHeight = LabelHeight;
				sc.Panel2Collapsed = true;
				sc.FixedPanel = FixedPanel.Panel2;
			}

			parent.Controls.Add(this); // Parent will have to move it into the right place.
			SetSplitPosition();

			// Don'f fire off all those size changed event handlers, unless it is really needed.
			if (Height != newHeight)
				Height = newHeight;
			treeNode.ResumeLayout(false);
		}

		void treeNode_MouseLeave(object sender, EventArgs e)
		{
			Highlighted = false;
		}

		void treeNode_MouseEnter(object sender, EventArgs e)
		{
			Highlighted = true;
		}

		/// <summary>
		/// Attempt to set the split position, but do NOT modify the global setting for
		/// the data tree if unsuccessful. This occurs during window initialization, since
		/// (I think) slices are created before the proper width is set for the containing
		/// data pane, and the constraints on the width of the splitter may not allow it to
		/// take on the persisted position.
		/// </summary>
		internal void SetSplitPosition()
		{
			SplitContainer sc = SplitCont;
			if (sc.IsSplitterFixed)
				return;

			int valueSansLabelindent = ContainingDataTree.SliceSplitPositionBase;
			int correctSplitPosition = valueSansLabelindent + LabelIndent();
			if (sc.SplitterDistance != correctSplitPosition)
			{
				sc.SplitterDistance = correctSplitPosition;

				int labelIndent = LabelIndent();
				//if ((sc.SplitterDistance > MaxAbbrevWidth && valueSansLabelindent <= MaxAbbrevWidth)
				//	|| (sc.SplitterDistance <= MaxAbbrevWidth && valueSansLabelindent > MaxAbbrevWidth))
				//{
					TreeNode.Invalidate();
				//}
			}
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			CheckDisposed();

			// Skip handling this, if the DataTree hasn't
			// set the official width using SetWidthForDataTreeLayout
			if (!m_widthHasBeenSetByDataTree)
				return;

			base.OnSizeChanged(e);
			// When the size changes, we MAY be able to adjust the split position to what
			// the data tree remembers as the right position.
			SetSplitPosition();
		}

		/// <summary>
		/// If we don't have a splitter (because no label), set the width of the
		/// tree node directly; the other node's size is set by being docked 'fill'.
		/// </summary>
		/// <param name="levent"></param>
		protected override void OnLayout(LayoutEventArgs levent)
		{
			CheckDisposed();

			if (SplitCont.Panel2Collapsed)
				TreeNode.Width = LabelIndent();

			base.OnLayout(levent);
		}

		/// <summary>
		/// Indicates whether this is an active slice, which means it displays extra
		/// controls. Currently only SummarySlices can be active.
		/// (We could use this for the context menu icon, but that only shows on
		/// the actual current slice, whereas several slices may show commands.)
		/// </summary>
		public virtual bool Active
		{
			get
			{
				CheckDisposed();

				return false;
			}
			set
			{
				CheckDisposed();
			}
		}

		/// <summary>
		/// Become active if you are a parent of the specified child; otherwise become inactive.
		/// </summary>
		/// <param name="child"></param>
		public virtual void BecomeActiveIfParent(Slice child)
		{
			CheckDisposed();
		}

		#region IDisposable override

		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(this.ToString() + this.GetHashCode().ToString(), "Trying to use object that has been disposed.");
		}

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
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;
			if (Disposing)
				return; // Should throw, to let us know to use DestroyHandle, before calling base method.

			if (disposing)
			{
				// Dispose managed resources here.
				SplitCont.SplitterMoved -= new SplitterEventHandler(mySplitterMoved);
				// If anyone but the owning DataTree called this to be disposed,
				// then it will still hold a referecne to this slice in an event handler.
				// We could take care of it here by asking the DT to renove it,
				// but I (RandyR) am inclined to not do that, since
				// only the DT is really authorized to dispose its slices.

				if (m_fontLabel != null)
					m_fontLabel.Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_fontLabel = null;
			m_smallImages = null; // Owned by the property table or XWindow, so just make it null;
			m_stringTable = null;
			m_cache = null;
			m_key = null;
			m_obj = null;
			m_callerNode = null;
			m_configurationNode = null;
			m_mediator = null;
			m_configurationParameters = null;
			m_strLabel = null;
			m_strAbbr = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		/// <summary>
		/// This method determines how much we should indent nodes produced from "part ref"
		/// elements embedded inside an "indent" element in another "part ref" element.
		/// Currently, by default we in fact do NOT add any indent, unless there is also
		/// an attribute indent="true".
		/// </summary>
		/// <param name="indentNode"></param>
		/// <returns>0 for no indent, 1 to indent.</returns>
		internal static int ExtraIndent(XmlNode indentNode)
		{
			return XmlUtils.GetOptionalBooleanAttributeValue(indentNode, "indent", false) ? 1 : 0;
		}

		public virtual void GenerateChildren(XmlNode node, XmlNode caller, ICmObject obj, int indent, ref int insPos, ArrayList path, ObjSeqHashMap reuseMap)
		{
			CheckDisposed();

			// If node has children, figure what to do with them...
			XmlNodeList children = node.ChildNodes;
			DataTree.NodeTestResult ntr = DataTree.NodeTestResult.kntrNothing;
			// We may get child nodes from either the node itself or the calling part, but currently
			// don't try to handle both; we consider the children of the caller, if any, to override
			// the children of the node (but not unify with them, since a different kind of children
			// are involved).
			// A newly created slice is always in state ktisFixed, but that is not appropriate if it
			// has children from either source. However, a node which notionally has children may in fact have nothing to
			// show, perhaps because a sequence is empty. First evaluate this, and if true, set it
			// to ktisCollapsedEmpty.
			//bool fUseChildrenOfNode;
			XmlNode indentNode = null;
			if (caller != null)
				indentNode = caller.SelectSingleNode("indent");
			if (indentNode != null)
			{
				// Similarly pretest for children of caller, to see whether anything is produced.
				ContainingDataTree.ApplyLayout(obj, indentNode, indent + ExtraIndent(indentNode), insPos, path, reuseMap, true, out ntr);
				//fUseChildrenOfNode = false;
			}
			else
			{
				int insPosT = insPos; // don't modify the real one in this test call.
				ntr = ContainingDataTree.ProcessPartChildren(node, path, reuseMap, obj, indent + ExtraIndent(node), ref insPosT, true, null, false, node);
				//fUseChildrenOfNode = true;
			}

			if (ntr == DataTree.NodeTestResult.kntrNothing)
				Expansion = DataTree.TreeItemState.ktisFixed; // probably redundant, but play safe
			else if (ntr == DataTree.NodeTestResult.kntrPossible)
			{
				// It could have children but currently can't: we always show this as collapsedEmpty.
				Expansion = DataTree.TreeItemState.ktisCollapsedEmpty;
			}
			// Remaining branches are for a node that really has children.
			else if (Expansion == DataTree.TreeItemState.ktisCollapsed)
			{
				// Reusing a node that was collapsed (and it has something to expand):
				// leave it that way (whatever the spec says).
			}
			else if (XmlUtils.GetOptionalAttributeValue(node, "expansion") != "doNotExpand")
			// Old code does not expand by default, couple of ways to override.
			//			else if (Expansion == DataTree.TreeItemState.ktisExpanded
			//				|| (fUseChildrenOfNode && XmlUtils.GetOptionalAttributeValue(node, "expansion") == "expanded")
			//				|| (XmlUtils.GetOptionalAttributeValue(caller, "expansion") == "expanded")
			//				|| Expansion == DataTree.TreeItemState.ktisCollapsedEmpty)
			{
				// Either re-using a node that was expanded, or the slice spec says to auto-expand,
				// or the calling element says to auto-expand,
				// or a node that was previously empty now has real children, probably a first object
				// that was just added that the user wants to see:
				// fill in the children.
				Expansion = DataTree.TreeItemState.ktisExpanded;
				CreateIndentedNodes(caller, obj, indent, ref insPos, path, reuseMap, node);
			}
			else
			{
				// Either a new node or one previously collapsedEmpty. But it now definitely has children,
				// so neither of those states is appropriate. And we've covered all the cases where it
				// ought to be expanded. So show it as collapsed.
				Expansion = DataTree.TreeItemState.ktisCollapsed;
			}
		}

		public virtual void CreateIndentedNodes(XmlNode caller, ICmObject obj, int indent, ref int insPos,
			ArrayList path, ObjSeqHashMap reuseMap, XmlNode node)
		{
			CheckDisposed();

			string parameter = null;
			if (caller != null)
				parameter = XmlUtils.GetOptionalAttributeValue(caller, "param");
			XmlNode indentNode = null;
			if (caller != null)
				indentNode = caller.SelectSingleNode("indent");
			if (indentNode != null)
			{
				DataTree.NodeTestResult ntr;
				insPos = ContainingDataTree.ApplyLayout(obj, indentNode, indent + ExtraIndent(indentNode),
					insPos, path, reuseMap, false, out ntr);
			}
			else
				ContainingDataTree.ProcessPartChildren(node, path, reuseMap, obj, indent + ExtraIndent(node), ref insPos, false,
					parameter, false, caller);
		}

		#endregion Tree management

		#region Tree Display

		// Delegation methods (mainly or entirely duplicate similar methods on embedded control).
		public virtual int LabelHeight
		{
			get
			{
				CheckDisposed();
				return m_fontLabel.Height;
			}
		}

		/// <summary>
		/// Determines how deeply indented this item is in the tree diagram. 0 means no indent.
		/// </summary>
		public int Indent
		{
			get
			{
				CheckDisposed();

				return m_indent;
			}
			set
			{
				CheckDisposed();

				m_indent = value;
			}
		}
		// Return the expansion state of tree nodes.
		// This must be overwritten for editors that can expand.
		// @return A tree state enum.
		public DataTree.TreeItemState Expansion
		{
			get
			{
				CheckDisposed();

				return m_expansion;
			}
			set
			{
				CheckDisposed();

				m_expansion = value;
			} // Todo JohnT: real version may not have setter.
		}

		/// <summary>
		/// Gets and sets the label used to identify the item in the tree diagram.
		/// May need to override if not using the standard variable to draw a simple label
		/// </summary>
		public virtual string Label
		{
			get
			{
				CheckDisposed();

				return m_strLabel;
			}
			set
			{
				CheckDisposed();

				m_strLabel = value;
				//				this.Control.AccessibleName = m_strLabel;
				//				this.Control.AccessibilityObject.Value = m_strLabel;
			}
		}

		public string Abbreviation
		{
			get
			{
				CheckDisposed();

				return m_strAbbr;
			}
			set
			{
				CheckDisposed();

				m_strAbbr = value;
				if ((m_strAbbr == null || m_strAbbr.Length == 0) && m_strLabel != null)
				{
					int len = m_strLabel.Length > 4 ? 4 : m_strLabel.Length;
					m_strAbbr = m_strLabel.Substring(0, len);
				}
			}
		}

		/// <summary>
		/// Text to display as tooltip for label (SliceTreeNode).
		/// Defaults to Label.
		/// </summary>
		public string ToolTip
		{
			get
			{
				CheckDisposed();

				StringTable tbl = null;
				if (m_mediator != null && m_mediator.HasStringTable)
					tbl = m_mediator.StringTbl;
				return XmlUtils.GetLocalizedAttributeValue(tbl, m_configurationNode, "tooltip", Label);
			}
		}

		/// <summary>
		/// Help Topic ID for the slice
		/// </summary>
		public string HelpTopicID
		{
			get
			{
				CheckDisposed();

				return XmlUtils.GetOptionalAttributeValue(m_configurationNode, "helpTopicID");
			}
		}

		public virtual void DrawLabel(int x, int y, Graphics gr, int clipWidth)
		{
			CheckDisposed();

			if (SmallImages != null)
			{
				Image image = null;
				if (IsSequenceNode)
				{
					image = SmallImages.GetImage("sequence");
				}
				else if (IsCollectionNode)
				{
					image = SmallImages.GetImage("collection");
				}
				else if (IsObjectNode || WrapsAtomic)
				{
					image = SmallImages.GetImage("atomic");
				}
				if (image != null)
				{
					((Bitmap)image).MakeTransparent(System.Drawing.Color.Fuchsia);
					gr.DrawImage(image, x, y);
					x += image.Width;
				}
			}
			PointF p = new PointF(x, y);
			using (Brush brush = new SolidBrush(Color.FromKnownColor(KnownColor.ControlDarkDark)))
			{
				//			if (ContainingDataTree.CurrentSlice == this)
				//				brush = new SolidBrush(Color.Blue);
				string label = Label;
				if (SplitCont.SplitterDistance <= MaxAbbrevWidth)
					label = Abbreviation;
				gr.DrawString(label, m_fontLabel, brush, p);
				//			if(m_menuController != null)
				//				m_menuController.DrawAffordance(m_isHighlighted, x,y,gr,clipWidth);
			}
		}

		/// <summary>
		/// Returns the height, from the top of the item, at which to draw the line across towards it.
		/// Typically this is the center of where DrawLabel will draw the label, but it might not be (e.g.,
		/// if DrawLabel actually draws two labels and a bit of tree diagram).
		/// </summary>
		public virtual int GetBranchHeight()
		{
			CheckDisposed();

			return Convert.ToInt32((m_fontLabel.GetHeight() + 1.0) / 2.0);
		}

		public void Expand()
		{
			CheckDisposed();

			Expand(this.IndexInContainer);
		}

		public int IndexInContainer
		{
			get
			{
				CheckDisposed();

				return Parent.Controls.IndexOf(this);
			}
		}

		/// <summary>
		/// Expand this node, which is at position iSlice in its parent.
		/// </summary>
		/// <remarks> I (JH) don't know why this was written to take the index of the slice.
		/// It's just as easy for this class to find its own index.
		/// JohnT: for performance; finding its own index is a linear search,
		/// and the caller often has the info already, especially in loops expanding many children.</remarks>
		/// <param name="iSlice"></param>
		public virtual void Expand(int iSlice)
		{
			CheckDisposed();

			try
			{
				Debug.Assert(Expansion != DataTree.TreeItemState.ktisFixed);
				ContainingDataTree.DeepSuspendLayout();
				XmlNode caller = null;
				if (Key.Length > 1)
					caller = Key[Key.Length - 2] as XmlNode;
				int insPos = iSlice + 1;
				CreateIndentedNodes(caller, m_obj, Indent, ref insPos, new ArrayList(Key), new ObjSeqHashMap(), m_configurationNode);
				Expansion = DataTree.TreeItemState.ktisExpanded;
				// A crude way to force the +/- icon to be redrawn.
				// If this gets flashy, we could figure a smaller region to invalidate.
				ContainingDataTree.Invalidate(true);  // Invalidates both children.
			}
			finally
			{
				ContainingDataTree.DeepResumeLayout();
			}
		}

		/// <summary>
		/// Collapse this node, which is at position iSlice in its parent.
		/// </summary>
		/// <param name="iSlice"></param>
		public virtual void Collapse(int iSlice)
		{
			CheckDisposed();

			int iNextSliceNotChild = iSlice + 1;
			while (iNextSliceNotChild < Parent.Controls.Count
				&& ContainingDataTree.FieldOrDummyAt(iNextSliceNotChild).Indent > this.Indent)
			{
				iNextSliceNotChild++;
			}
			int count = iNextSliceNotChild - iSlice - 1;
			while (count > 0)
			{
				Slice goner = (Slice)Parent.Controls[iSlice + 1];
				Parent.Controls.Remove(goner);
				goner.Dispose();
				count--;
			}
			Expansion = DataTree.TreeItemState.ktisCollapsed;
			ContainingDataTree.PerformLayout();
			ContainingDataTree.Invalidate(true);  // Invalidates all children.
		}

		/// <summary>
		/// Collapse this node, in the sense of deleting subsequent nodes for which this is the masterslice.
		/// </summary>
		/// <param name="iSlice"></param>
		public virtual void CollapseMaster()
		{
			CheckDisposed();

			int iSlice = IndexInContainer;
			int iNextSliceNotChild = iSlice + 1;
			while (iNextSliceNotChild < ContainingDataTree.Controls.Count
				&& ContainingDataTree.FieldOrDummyAt(iNextSliceNotChild).MasterSlice == this)
			{
				iNextSliceNotChild++;
			}
			int count = iNextSliceNotChild - iSlice - 1;
			while (count > 0)
			{
				Slice goner = (Slice)Parent.Controls[iSlice + 1];
				Parent.Controls.Remove(goner);
				goner.Dispose();
				count--;
			}
			Expansion = DataTree.TreeItemState.ktisCollapsed;
			ContainingDataTree.PerformLayout();
			ContainingDataTree.Invalidate(true);  // Invalidates all children.
		}

		/// <summary>
		/// Record the slice that 'owns' this one (typically this was created by a CreateIndentedNodes call on the
		/// master slice). Most slices don't keep track of this, but this provides a common point for methods that
		/// work on slices that do.
		/// </summary>
		public virtual Slice MasterSlice
		{
			get
			{
				CheckDisposed();

				return null;
			}
			set
			{
				CheckDisposed();

				Debug.Assert(false);
			}
		}

		public virtual bool HandleMouseDown(Point p)
		{
			CheckDisposed();

			ContainingDataTree.CurrentSlice = this;
			if (ShowContextMenu != null)// m_btnRectangle.Contains(p))
			{
				ShowContextMenu(this, new TreeNodeEventArgs(TreeNode, this, p));
				return true;
			}
			else
				return false;
		}

		public virtual int LabelIndent()
		{
			CheckDisposed();

			return SliceTreeNode.kdxpLeftMargin +
				(Indent + 1) * SliceTreeNode.kdxpIndDist;
		}

		/// <summary>
		/// Draws the label in the containing SilTreeControl's Graphics object at the specified position.
		/// Override if you have a more complex type of label, e.g., if the field contains interlinear
		/// data and you want to label each line.
		/// </summary>
		public virtual void DrawLabel(int y, Graphics gr, int clipWidth)
		{
			CheckDisposed();

			DrawLabel(LabelIndent(), y, gr, clipWidth);
		}

		public bool IsObjectNode
		{
			get
			{
				CheckDisposed();

				string sClassName = Cache.GetClassName((uint)Object.ClassID);
				return
					(ConfigurationNode.SelectSingleNode("node") != null ||
					("PhCode" == sClassName) || // This is a hack to get one case to work that should be handled by the todo in the next comment (hab 2004.01.16 )
					false)	// todo: this should tell if the attr (not the nested one) is to a basic type or a cmobject
					&&
					ConfigurationNode.SelectSingleNode("seq") == null &&
					//MoAlloAdhocProhib.adjacency is the top-level node, but it's not really an object that you should be able to delete
					this.Object.Hvo != this.ContainingDataTree.RootObjectHvo;
			}
		}

		#endregion Tree Display

		#region Miscellaneous data methods

		/// <summary>
		/// Get the context for this slice considered as a member of a sequence.
		/// That is, if the current node is, at some level, a member of an owning sequence,
		/// find the most local such sequence, and return information indicating the position
		/// of the object this slice is part of, as well as the object and property that owns
		/// it.
		/// </summary>
		/// <param name="hvoOwner">Owner of the object this slice is part of.</param>
		/// <param name="flid">Owning sequence property this is part of.</param>
		/// <param name="ihvoPosition">Position of this object in owning sequence;
		/// or current position in cache, if a collection.</param>
		/// <returns>true if this slice is part of an owning sequence property.</returns>
		public bool GetSeqContext(out int hvoOwner, out int flid, out int ihvoPosition)
		{
			CheckDisposed();

			hvoOwner = 0; // compiler insists it be assigned.
			flid = 0;
			ihvoPosition = 0;

			if (m_key == null)
				return false;

			for (int inode = m_key.Length; --inode >= 0; )
			{
				object objNode = m_key[inode];
				if (objNode is XmlNode)
				{
					XmlNode node = (XmlNode)objNode;
					if (node.Name == "seq")
					{
						// if this is the last index, we don't have an hvo of anything to edit.
						if (inode == m_key.Length - 1)
							return false;

						// got it!
						// The next thing we push into key right after the "seq" node is always the
						// HVO of the particular item we're editing.
						int hvoItem = (int)(m_key[inode + 1]);
						string attrName = node.Attributes["field"].Value;
						FDO.FdoCache cache = ContainingDataTree.Cache;
						IFwMetaDataCache mdc = cache.MetaDataCacheAccessor;
						ICmObject obj = CmObject.CreateFromDBObject(cache, hvoItem);
						hvoOwner = obj.OwnerHVO;
						int clsid = cache.GetClassOfObject(hvoOwner);
						flid = (int)mdc.GetFieldId2((uint)clsid, attrName, true);
						if (flid == 0)
							return false;
						ihvoPosition = cache.GetObjIndex(hvoOwner, flid, hvoItem);
						return true;
					}
				}
			}
			return false;
		}

		/// <summary>
		/// Get the context for this slice considered as part of an owning atomic attr.
		/// That is, if the current node is, at some level, a member of an owning atomic attr,
		/// find the most local such attr, and return information indicating the object
		/// and property that owns it.
		/// </summary>
		/// <param name="hvoOwner">Owner of the object this slice is part of.</param>
		/// <param name="flid">Owning atomic property this is part of.</param>
		/// <returns>true if this slice is part of an owning atomic property.</returns>
		public bool GetAtomicContext(out int hvoOwner, out int flid)
		{
			CheckDisposed();

			// Compiler requires values to be set, but these are meaningless.
			hvoOwner = 0;
			flid = 0;
			if (m_key == null)
				return false;

			for (int inode = m_key.Length; --inode >= 0; )
			{
				object objNode = m_key[inode];
				if (objNode is XmlNode)
				{
					XmlNode node = (XmlNode)objNode;
					if (node.Name == "atomic")
					{
						// got it!
						// The next thing we push into key right after the "atomic" node is always the
						// HVO of the particular item we're editing.
						int hvoItem = (int)(m_key[inode + 1]);
						string attrName = node.Attributes["field"].Value;
						FDO.FdoCache cache = ContainingDataTree.Cache;
						IFwMetaDataCache mdc = cache.MetaDataCacheAccessor;
						ICmObject objOwner = CmObject.CreateFromDBObject(cache, hvoItem);
						hvoOwner = objOwner.OwnerHVO;
						int clsid = cache.GetClassOfObject(hvoOwner);
						flid = (int)mdc.GetFieldId2((uint)clsid, attrName, true);
						return true;
					}
				}
			}
			return false;
		}

		/// <summary>
		/// Get the flid for the specified field name of this slice's object. May return zero if
		/// the object does not have that field.
		/// </summary>
		/// <param name="fieldName"></param>
		/// <returns></returns>
		protected uint GetFlid(string fieldName)
		{
			CheckDisposed();

			uint flid = m_cache.MetaDataCacheAccessor.GetFieldId2((uint)this.m_obj.ClassID, fieldName, true);
			//Debug.Assert( flid !=0);
			return flid;
		}

		protected int GetFieldType(uint flid)
		{
			CheckDisposed();

			int type = m_cache.MetaDataCacheAccessor.GetFieldType(flid);
			return type;
		}

		#endregion Miscellaneous data methods

		#region Menu Command Handlers

		/// <summary>
		/// do an insertion
		/// </summary>
		/// <remarks> called by the containing environment in response to a user command.</remarks>
		public virtual void HandleInsertCommand(string fieldName, string className)
		{
			CheckDisposed();

			HandleInsertCommand(fieldName, className, null, null);
		}

		/// <summary>
		/// Answer whether clidTest is, or is a subclass of, clidSig.
		/// That is, either clidTest is the same as clidSig, or one of the base classes of clidTest is clidSig.
		/// As a special case, if clidSig is 0, all classes are considered to match
		/// </summary>
		/// <param name="clidTest"></param>
		/// <param name="clidSig"></param>
		/// <returns></returns>
		bool IsOrInheritsFrom(uint clidTest, uint clidSig)
		{
			CheckDisposed();

			return Cache.ClassIsOrInheritsFrom(clidTest, clidSig);
		}

		/// <summary>
		/// do an insertion
		/// </summary>
		/// <remarks> called by the containing environment in response to a user command.</remarks>
		/// <param name="fieldName">name of field to create in</param>
		/// <param name="className">class of object to create</param>
		/// <param name="ownerClassName">class of expected owner. If the current slice's object is not
		/// this class (or a subclass), look for a containing object that is.</param>
		/// <param name="recomputeVirtual">if non-null, this is a virtual property that should be updated for all
		/// moved objects and their descendents of the specified class (string has form class.property)</param>
		public virtual void HandleInsertCommand(string fieldName, string className, string ownerClassName, string recomputeVirtual)
		{
			CheckDisposed();

			uint newObjectClassId = m_cache.MetaDataCacheAccessor.GetClassId(className);
			if (newObjectClassId == 0)
				throw new ArgumentException("There does not appear to be a database class named '" + className + "'.");

			uint ownerClassId = 0;
			if (ownerClassName != null && ownerClassName != "")
			{
				ownerClassId = Cache.MetaDataCacheAccessor.GetClassId(ownerClassName);
				if (ownerClassId == 0)
					throw new ArgumentException("There does not appear to be a database class named '" + ownerClassName + "'.");
			}
			// First see whether THIS slice can do it. This helps us insert in the right position for things like
			// subsenses.
			if (InsertObjectIfPossible(newObjectClassId, ownerClassId, fieldName, this, recomputeVirtual))
				return;
			// The previous call may have done the insert, but failed to recognize it due to disposing of the slice
			// during a PropChanged operation.  See LT-9005.
			if (IsDisposed)
				return;

			// See if any direct ancestor can do it.
			int index = IndexInContainer;
			for (int i = index - 1; i >= 0; i--)
			{
				if (InsertObjectIfPossible(newObjectClassId, ownerClassId, fieldName, (Slice)Parent.Controls[i], recomputeVirtual))
					return;
			}

			// Loop through all slices until we find a slice whose object is of the right class
			// and that has the specified field.
			foreach (Slice slice in Parent.Controls)
			{
				Debug.WriteLine(String.Format("HandleInsertCommand({0}, {1}, {2}, {3}) -- slice = {4}",
					fieldName, className, ownerClassName, recomputeVirtual, slice.ToString()));
				if (InsertObjectIfPossible(newObjectClassId, ownerClassId, fieldName, slice, recomputeVirtual))
					break;
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="newObjectClassId"></param>
		/// <param name="ownerClassId"></param>
		/// <param name="fieldName"></param>
		/// <param name="slice"></param>
		/// <param name="recomputeVirtual"></param>
		/// <returns></returns>
		/// <remarks>
		/// 'true' means we found a suitable place to insert an object,
		/// not that it was actually inserted. It may, or may not, have been inserted in this case.
		/// 'false' means no suitable place was found, so the calling code can try other locations.
		/// </remarks>
		private bool InsertObjectIfPossible(uint newObjectClassId, uint ownerClassId, string fieldName, Slice slice, string recomputeVirtual)
		{
			if ((ownerClassId > 0 && IsOrInheritsFrom((uint)(slice.Object.ClassID), ownerClassId)) // For adding senses using the simple edit mode, no matter where the cursor is.
				|| slice.Object == Object
				//|| slice.Object == ContainingDataTree.Root)
				|| slice.Object.Equals(ContainingDataTree.Root)) // Other cases.
			{
				// The slice's object has an acceptable type provided it implements the required field.
				// See if the current slice's object has the field named.
				uint flid = slice.GetFlid(fieldName);
				uint flidT = m_cache.MetaDataCacheAccessor.GetFieldId2((uint)ownerClassId, fieldName, true);
				if (flidT != 0 && flid != flidT)
					flid = flidT;
				if (flid == 0)
					return false;
				// Found a suitable slice. Do the insertion.
				IFwMetaDataCache mdc = Cache.MetaDataCacheAccessor;
				int insertionPosition = -1;		// causes return false if not changed.
				if (m_cache.IsReferenceProperty((int)flid))
				{
					insertionPosition = Slice.InsertObjectIntoVirtualBackref(Cache, m_mediator,
						Cache.VwCacheDaAccessor.GetVirtualHandlerId((int)flid), slice.Object.Hvo,
						newObjectClassId, ownerClassId, flid);
				}
				else
				{
					insertionPosition = slice.InsertObject(flid, newObjectClassId);
				}
				if (insertionPosition < 0)
					return insertionPosition == -2;		// -2 keeps dlg for adding subPOSes from firing for each slice when cancelled.
				if (String.IsNullOrEmpty(recomputeVirtual))
					return true;
				// Figure the things to recompute.
				int hvoOwner = slice.Object.Hvo;
				string[] parts = recomputeVirtual.Split('.');
				if (parts.Length != 2)
				{
					Debug.Assert(parts.Length == 2);
					return true; // but fairly harmless to ignore
				}
				uint clidVirtual = mdc.GetClassId(parts[0]);
				int flidVirtual = (int)mdc.GetFieldId2(clidVirtual, parts[1], true);
				ISilDataAccess sda = Cache.MainCacheAccessor;
				int chvo = sda.get_VecSize(hvoOwner, (int)flid);
				IVwVirtualHandler vh = Cache.VwCacheDaAccessor.GetVirtualHandlerId(flidVirtual);
				int typeVirtual = mdc.GetFieldType((uint)flidVirtual);
				if (vh == null)
					return true; // not a virtual property.
				for (int i = insertionPosition + 1; i < chvo; i++)
				{
					RecomputeVirtuals(sda.get_VecItem(hvoOwner, (int)flid, i), clidVirtual, flidVirtual, typeVirtual, mdc, sda, vh);
				}

				return true;
			}
			return false;
		}

		static internal int InsertObjectIntoVirtualBackref(FdoCache cache, Mediator mediator, IVwVirtualHandler vh,
			int hvoSlice, uint clidNewObj, uint clidOwner, uint flid)
		{
			if (vh != null)
			{
				int clidSlice = cache.GetClassOfObject(hvoSlice);
				if (clidNewObj == LexEntry.kclsidLexEntry &&
					clidSlice == LexEntry.kclsidLexEntry &&
					clidOwner == LexDb.kclsidLexDb)
				{
					if (vh.FieldName == "VariantFormEntryBackRefs")
					{
						using (InsertVariantDlg dlg = new InsertVariantDlg())
						{
							ILexEntry entOld = LexEntry.CreateFromDBObject(cache, hvoSlice);
							dlg.SetHelpTopic("khtpInsertVariantDlg");
							dlg.SetDlgInfo(cache, mediator, entOld as IVariantComponentLexeme);
							if (dlg.ShowDialog() == DialogResult.OK && dlg.NewlyCreatedVariantEntryRefResult)
							{
								int insertPos = cache.GetVectorSize(hvoSlice, (int)flid);
								cache.PropChanged(hvoSlice, (int)flid, insertPos, 1, 0);
								return insertPos;
							}
							// say we've handled this.
							return -2;
						}
					}
				}
			}
			return -1;
		}

		/// <summary>
		/// If object hvo has no cached value for the property flidVirtual, do nothing.
		/// Otherwise, compute a new value for the property, and issue a PropChanged. (Currently only string type supported)
		/// If it has owning properties of type clidVirtual, do the same for all their items.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="flidVirtual"></param>
		/// <param name="mdc"></param>
		/// <param name="sda"></param>
		private void RecomputeVirtuals(int hvo, uint clidVirtual, int flidVirtual, int typeVirtual, IFwMetaDataCache mdc, ISilDataAccess sda,
			IVwVirtualHandler vh)
		{
			if (Cache.GetClassOfObject(hvo) != clidVirtual)
				return;
			// Unless it's a computeEveryTime property, we don't need to worry if it's not already cached.
			if (vh.ComputeEveryTime || sda.get_IsPropInCache(hvo, flidVirtual, typeVirtual, 0))
			{
				vh.Load(hvo, flidVirtual, 0, Cache.VwCacheDaAccessor);
				switch (typeVirtual)
				{
					case (int)CellarModuleDefns.kcptString:
						sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll, hvo, flidVirtual, 0, 0, 0);
						break;
					default:
						Debug.WriteLine("RecomputeVirtuals: unimplemented prop type");
						break;
				}
			}
			uint[] flids = DbOps.GetFieldsInClassOfType(mdc, (int)clidVirtual, FieldType.kgrfcptOwning);
			foreach (uint flid in flids)
			{
				int type = mdc.GetFieldType(flid);
				if (type == (int)CellarModuleDefns.kfcptOwningAtom)
				{
					RecomputeVirtuals(sda.get_ObjectProp(hvo, (int)flid), clidVirtual, flidVirtual, typeVirtual, mdc, sda, vh);
				}
				else
				{
					// must be owning sequence or collection; do them all.
					int chvo = sda.get_VecSize(hvo, (int)flid);
					for (int i = 0; i < chvo; i++)
						RecomputeVirtuals(sda.get_VecItem(hvo, (int)flid, i), clidVirtual, flidVirtual, typeVirtual, mdc, sda, vh);

				}
			}
		}

		Slice NextSlice
		{
			get
			{
				CheckDisposed();

				int indexOfThis = this.IndexInContainer;
				if (indexOfThis < ContainingDataTree.Controls.Count - 1)
					return Parent.Controls[indexOfThis + 1] as Slice;
				else
					return null;
			}
		}

		/// <summary>
		/// Insert a new object of the specified class into the specified property of your object.
		/// </summary>
		/// <param name="flid"></param>
		/// <param name="newObjectClassId"></param>
		/// <returns>-1 if unsuccessful -2 if unsuccessful and no further attempts should be made,
		/// otherwise, index of new object (0 if collection)</returns>
		int InsertObject(uint flid, uint newObjectClassId)
		{
			CheckDisposed();

			bool fAbstract = m_cache.MetaDataCacheAccessor.GetAbstract(newObjectClassId);
			if (fAbstract)
			{
				// We've been handed an abstract class to insert.  Try to determine the desired
				// concrete from the context.
				if (newObjectClassId == MoForm.kclsidMoForm && Object is LexEntry)
				{
					ILexEntry entry = (Object as ILexEntry);
					newObjectClassId = (uint)entry.GetDefaultClassForNewAllomorph();
				}
				else
				{
					return -1;
				}
			}
			// OK, we can add to property flid of the object of slice slice.
			int insertionPosition = 0;//leave it at 0 if it does not matter
			int hvoOwner = Object.Hvo;
			int clidOwner = m_cache.GetClassOfObject(hvoOwner);
			int clidOfFlid = (int)(flid / 1000);
			if (clidOwner != clidOfFlid && clidOfFlid == m_cache.GetClassOfObject(Object.OwnerHVO))
			{
				hvoOwner = Object.OwnerHVO;
				clidOwner = clidOfFlid;
			}
			int type = GetFieldType(flid);
			if (type == (int)FDO.FieldType.kcptOwningSequence)
			{
				insertionPosition = Cache.GetVectorSize(hvoOwner, (int)flid);
				if (ContainingDataTree != null && ContainingDataTree.CurrentSlice != null)
				{
					ISilDataAccess sda = m_cache.MainCacheAccessor;
					int chvo = sda.get_VecSize(hvoOwner, (int)flid);
					// See if the current slice in any way indicates a position in that property.
					object[] key = ContainingDataTree.CurrentSlice.Key;
					bool fGotIt = false;
					for (int ikey = key.Length - 1; ikey >= 0 && !fGotIt; ikey--)
					{
						if (!(key[ikey] is int))
							continue;
						int hvoTarget = (int)key[ikey];
						for (int i = 0; i < chvo; i++)
						{
							if (hvoTarget == sda.get_VecItem(hvoOwner, (int)flid, i))
							{
								insertionPosition = i + 1; // insert after current object.
								fGotIt = true; // break outer loop
								break;
							}
						}
					}
				}
			}
			Set<Slice> slices = new Set<Slice>(Parent.Controls.Count);
			foreach (Slice slice in Parent.Controls)
				slices.Add(slice);

			// Save DataTree for the finally block.  Note premature return below due to IsDisposed.  See LT-9005.
			DataTree dtContainer = ContainingDataTree;
			try
			{
				dtContainer.SetCurrentObjectFlids(hvoOwner, (int)flid);
				using (CmObjectUi uiObj = CmObjectUi.CreateNewUiObject(m_mediator, newObjectClassId, hvoOwner, (int)flid, insertionPosition))
				{
					// If uiObj is null, typically CreateNewUiObject displayed a dialog and the user cancelled.
					// We return -1 to make the caller give up trying to insert, so we don't get another dialog if
					// there is another slice that could insert this kind of object.
					// If 'this' isDisposed, typically the inserted object occupies a place in the record list for
					// this view, and inserting an object caused the list to be refreshed and all slices for this
					// record to be disposed. In that case, we won't be able to find a child of this to activate,
					// so we'll just settle for having created the object.
					// Enhance JohnT: possibly we could load information from the slice into local variables before
					// calling CreateNewUiObject so that we could do a better job of picking the slice to focus
					// after an insert which disposes 'this'. Or perhaps we could improve the refresh list process
					// so that it more successfully restores the current item without disposing of all the slices.
					if (IsDisposed)
						return -1;
					if (uiObj == null)
						return -2; // Nothing created.


					//			if (ihvoPosition == ClassAndPropInfo.kposNotSet && cpi.fieldType == DataTree.kcptOwningSequence)
					//			{
					//				// insert at end of sequence.
					//				ihvoPosition = cache.GetVectorSize(hvoOwner, (int)cpi.flid);
					//			} // otherwise we already worked out the position or it doesn't matter
					//			// Note: ihvoPosition ignored if sequence(?) or atomic.
					//			int hvoNew = cache.CreateObject((int)(cpi.signatureClsid), hvoOwner, (int)(cpi.flid), ihvoPosition);
					//			cache.MainCacheAccessor.PropChanged(null, (int)PropChangeType.kpctNotifyAll, hvoOwner, (int)(cpi.flid), ihvoPosition, 1, 0);
					if (hvoOwner == Object.Hvo && Expansion == DataTree.TreeItemState.ktisCollapsed)
					{
						// We added something to the object of the current slice...almost certainly it
						// will be something that will display under this node...if it is still collapsed,
						// expand it to show the thing inserted.
						TreeNode.ToggleExpansion(IndexInContainer);
					}
					Slice child = ExpandSubItem(uiObj.Object.Hvo);
					if (child != null)
						child.FocusSliceOrChild();
					else
					{
						// If possible, jump to the newly inserted sub item.
						if (m_mediator.BroadcastMessageUntilHandled("JumpToRecord", uiObj.Object.Hvo))
							return insertionPosition;
						// If we haven't found a slice...common now, because there's rarely a need to expand anything...
						// and some slice was added, focus it.
						foreach (Slice slice in Parent.Controls)
						{
							if (!slices.Contains(slice))
							{
								slice.FocusSliceOrChild();
								break;
							}
						}
					}
				}
			}
			finally
			{
				dtContainer.ClearCurrentObjectFlids();
			}
			return insertionPosition;
		}

		/// <summary>
		/// Find a slice nested below this one whose object is hvo and expand it if it is collapsed.
		/// </summary>
		/// <param name="hvo"></param>
		/// <returns>Slice for subitem, or null</returns>
		public Slice ExpandSubItem(int hvo)
		{
			CheckDisposed();

			int cslice = ContainingDataTree.Controls.Count;
			for (int islice = IndexInContainer + 1; islice < cslice; ++islice)
			{
				Slice slice = Parent.Controls[islice] as Slice;
				if (slice.Object.Hvo == hvo)
				{
					if (slice.Expansion == DataTree.TreeItemState.ktisCollapsed)
						slice.TreeNode.ToggleExpansion(islice);
					return slice;
				}
				// Stop if we get past the children of the current object.
				if (slice.Indent <= this.Indent)
					break;
			}
			return null;
		}

		/// <summary>
		/// Focus the specified slice (or the first of its children that can accept focus).
		/// </summary>
		public Slice FocusSliceOrChild()
		{
			CheckDisposed();

			// Make sure that preceding slices are real and visible.  Otherwise, the
			// inserted slice can be shown in the wrong place.  See LT-6306.
			int iLastRealVisible = 0;
			for (int i = IndexInContainer - 1; i >= 0; --i)
			{
				Slice slice = ContainingDataTree.FieldOrDummyAt(i) as Slice;
				if (slice.IsRealSlice && slice.Visible)
				{
					iLastRealVisible = i;
					break;
				}
			}
			// Be very careful...the call to FieldAt in this loop may dispose this!
			// Therefore almost any method call to this hereafter may crash.
			DataTree containingDT = ContainingDataTree;
			int myIndex = IndexInContainer;
			int myIndent = Indent;
			for (int i = iLastRealVisible + 1; i < IndexInContainer; ++i)
			{
				Slice slice = containingDT.FieldAt(i);	// make it real.
				if (!slice.Visible)								// make it visible.
					DataTree.MakeSliceVisible(slice);
			}
			int cslice = containingDT.Controls.Count;
			Slice sliceRetVal = null;
			for (int islice = myIndex; islice < cslice; ++islice)
			{
				Slice slice = containingDT.FieldAt(islice);
				DataTree.MakeSliceVisible(slice); // otherwise it can't take focus
				if (slice.TakeFocus(false))
				{
					sliceRetVal = slice;
					break;
				}
				// Stop if we get past the children of the current object.
				if (slice.Indent >= myIndent)
					break;
			}
			if (sliceRetVal != null)
			{
				int xDataTreeHeight = containingDT.Height;
				Point ptScrollPos = containingDT.AutoScrollPosition;
				int delta = (xDataTreeHeight / 4) - sliceRetVal.Location.Y;
				if (delta < 0)
					containingDT.AutoScrollPosition = new Point(-ptScrollPos.X, -ptScrollPos.Y - delta);
			}
			return sliceRetVal;
		}

		public virtual void HandleDeleteCommand(Command cmd)
		{
			CheckDisposed();

			bool fFromBackRef = FromVariantBackRefField;
			int hvo = GetObjectHvoForMenusToOperateOn();
			if (hvo <= 0)
				throw new ConfigurationException("Slice:GetObjectHvoForMenusToOperateOn is either messed up or should not have been called, because it could not find the object to be deleted.", m_configurationNode);
			else
			{
				DataTree dt = ContainingDataTree;
				try
				{
					dt.SetCurrentObjectFlids(hvo, 0);
					using (CmObjectUi ui = CmObjectUi.MakeUi(m_cache, hvo))
					{
						ui.Mediator = m_mediator;
						ui.DeleteUnderlyingObject();
					}
				}
				finally
				{
					dt.ClearCurrentObjectFlids();
				}
				if (fFromBackRef)
					PropChangeBackReference(cmd);
			}
			// The slice will likely be disposed in the DeleteUnderlyingObject call,
			// so make sure we aren't collected until we leave this method, at least.
			GC.KeepAlive(this);
		}

		/// <summary>
		/// Check whether a "Delete Reference" command can be executed.  Currently implemented
		/// only for the VariantEntryBackRefs / LexEntry/EntryRefs/ComponentLexemes references.
		/// </summary>
		/// <param name="cmd"></param>
		/// <returns></returns>
		public virtual bool CanDeleteReferenceNow(Command cmd)
		{
			CheckDisposed();
			return FromVariantBackRefField;
		}

		/// <summary>
		/// Handle a "Delete Reference" command.  Currently implemented only for the
		/// VariantEntryBackRefs / LexEntry/EntryRefs/ComponentLexemes references.
		/// </summary>
		/// <param name="cmd"></param>
		public virtual void HandleDeleteReferenceCommand(Command cmd)
		{
			CheckDisposed();
			if (this.NextSlice != null && this.NextSlice.Object != null)
			{
				LexEntryRef ler = this.NextSlice.Object as LexEntryRef;
				if (ler != null)
				{
					ler.ComponentLexemesRS.Remove(this.ContainingDataTree.RootObjectHvo);
					m_cache.PropChanged(ler.Hvo, (int)LexEntryRef.LexEntryRefTags.kflidComponentLexemes,
						0, ler.PrimaryLexemesRS.Count, 1);
					// probably not needed, but safe...
					if (ler.PrimaryLexemesRS.Contains(this.ContainingDataTree.RootObjectHvo))
					{
						ler.PrimaryLexemesRS.Remove(this.ContainingDataTree.RootObjectHvo);
						m_cache.PropChanged(ler.Hvo, (int)LexEntryRef.LexEntryRefTags.kflidPrimaryLexemes,
							0, ler.PrimaryLexemesRS.Count, 1);
					}
					PropChangeBackReference(cmd);
				}
			}
		}

		private void PropChangeBackReference(Command cmd)
		{
			string sField = cmd.GetParameter("field", null);
			string sClass = cmd.GetParameter("className", null);
			if (!String.IsNullOrEmpty(sField) && !String.IsNullOrEmpty(sClass))
			{
				uint flid = m_cache.MetaDataCacheAccessor.GetFieldId(sClass, sField, true);
				if (flid != 0)
				{
					m_cache.PropChanged(this.ContainingDataTree.RootObjectHvo, (int)flid, 0, 0, 1);
				}
			}
		}

		/// <summary>
		/// gives the object hvo hat should be the target of Delete, copy, etc. for menus operating on this slice label.
		/// </summary>
		/// <returns>return 0 if this slice is supposed to operate on an atomic field which is currently empty.</returns>
		public int GetObjectHvoForMenusToOperateOn()
		{
			CheckDisposed();

			if (WrapsAtomic)
			{
				XmlNodeList nodes = m_configurationNode.SelectNodes("atomic");
				if (nodes.Count != 1)
					throw new ConfigurationException("Expected to find a single <atomic> element in here", m_configurationNode);
				string field = XmlUtils.GetManditoryAttributeValue(nodes[0], "field");
				uint flid = GetFlid(field);
				Debug.Assert(flid != 0);
				return m_cache.GetObjProperty(m_obj.Hvo, (int)flid);
			}
			else if (FromVariantBackRefField)
			{
				return BackRefObjectHvo;
			}
			else
				return m_obj.Hvo;
		}

		/// <summary>
		/// Get the flid associated with this slice, if there is one.
		/// </summary>
		public virtual int Flid
		{
			get
			{
				if (m_configurationNode != null && m_obj != null)
				{
					string sField = XmlUtils.GetOptionalAttributeValue(m_configurationNode, "field");
					if (!String.IsNullOrEmpty(sField))
						return (int)GetFlid(sField);
				}
				return 0;
			}
		}

		private bool FromVariantBackRefField
		{
			get
			{
				int hvoRoot = ContainingDataTree.RootObjectHvo;
				int clidRoot = m_cache.GetClassOfObject(hvoRoot);
				return clidRoot == LexEntry.kclsidLexEntry &&
					Object != null && Object.Hvo != hvoRoot &&
					Object.OwnerHVO != 0 && Object.OwnerHVO != hvoRoot &&
					(m_cache.GetClassOfObject(Object.Hvo) == clidRoot || m_cache.GetClassOfObject(Object.OwnerHVO) == clidRoot);
			}
		}

		private int BackRefObjectHvo
		{
			get
			{
				int hvoRoot = this.ContainingDataTree.RootObjectHvo;
				int clidRoot = m_cache.GetClassOfObject(hvoRoot);
				if (clidRoot == LexEntry.kclsidLexEntry &&
					Object != null && Object.Hvo != hvoRoot &&
					Object.OwnerHVO != 0 && Object.OwnerHVO != hvoRoot)
				{
					if (m_cache.GetClassOfObject(Object.Hvo) == clidRoot)
					{
						return Object.Hvo;
					}
					else if (m_cache.GetClassOfObject(Object.OwnerHVO) == clidRoot)
					{
						return Object.OwnerHVO;
					}
				}
				return 0;
			}
		}

		/// <summary>
		/// is it possible to do a deletion menu command on this slice right now?
		/// </summary>
		/// <returns></returns>
		public bool GetCanDeleteNow()
		{
			CheckDisposed();

			int hvo = GetObjectHvoForMenusToOperateOn();
			if (hvo <= 0)
			{
				return false;
			}
			else
			{
			}

			ICmObject owner = CmObject.CreateFromDBObject(Cache, m_cache.GetOwnerOfObject(hvo));
			int flid = m_cache.GetOwningFlidOfObject(hvo);
			if (!owner.IsFieldRequired(flid))
				return true;

			//now, if the field is required, then we do not allow this to be deleted if it is atomic
			//futureTodo: this prevents the user from the deleting something in order to create something
			//of a different class, or to paste in other object in this field.
			if (!Cache.IsVectorProperty(flid))
				return false;
			else	//still OK to delete so long as it is not the last item.
				return Cache.GetVectorSize(owner.Hvo, flid) > 1;
		}

		/// <summary>
		/// Is it possible to do a merge menu command on this slice right now?
		/// </summary>
		/// <returns></returns>
		public bool GetCanMergeNow()
		{
			CheckDisposed();

			int hvo = GetObjectHvoForMenusToOperateOn();
			int clsid = Cache.GetClassOfObject(hvo);
			if (hvo <= 0)
				return false;

			ICmObject owner = CmObject.CreateFromDBObject(Cache, m_cache.GetOwnerOfObject(hvo));
			int flid = m_cache.GetOwningFlidOfObject(hvo);
			// No support yet for atomic properties.
			if (!Cache.IsVectorProperty(flid))
				return false;

			// Special handling for allomorphs, as they can be merged into the lexeme form.
			if (flid == (int)LexEntry.LexEntryTags.kflidAlternateForms)
			{
				// We can merge an alternate with the lexeme form,
				// if it is the same class.
				if (clsid == Cache.GetClassOfObject((owner as ILexEntry).LexemeFormOAHvo))
					return true;
			}
			// A subsense can always merge into its owning sense.
			if (flid == (int)LexSense.LexSenseTags.kflidSenses)
				return true;

			int vectorSize = Cache.GetVectorSize(owner.Hvo, flid);
			if (owner.IsFieldRequired(flid)
				&& vectorSize < 2)
				return false;

			// Check now to see if there are any other objects of the same class in the flid,
			// since only objects of the same class can be merged.
			foreach (int hvoInner in Cache.GetVectorProperty(owner.Hvo, flid, true))
			{
				if (hvoInner != hvo
					&& clsid == Cache.GetClassOfObject(hvoInner))
				{
					return true;
				}
			}

			return false;
		}

		public virtual void HandleMergeCommand(bool fLoseNoTextData)
		{
			CheckDisposed();

			int hvo = GetObjectHvoForMenusToOperateOn();
			if (hvo <= 0)
				throw new ConfigurationException("Slice:GetObjectHvoForMenusToOperateOn is either messed up or should not have been called, because it could not find the object to be merged.", m_configurationNode);

			using (CmObjectUi ui = CmObjectUi.MakeUi(m_cache, hvo))
			{
				ui.Mediator = m_mediator;
				ui.MergeUnderlyingObject(fLoseNoTextData);
			}
			// The slice will likely be disposed in the MergeUnderlyingObject call,
			// so make sure we aren't collected until we leave this method, at least.
			GC.KeepAlive(this);
		}

		/// <summary>
		/// Is it possible to do a split menu command on this slice right now?
		/// </summary>
		/// <returns></returns>
		public bool GetCanSplitNow()
		{
			CheckDisposed();

			int hvo = GetObjectHvoForMenusToOperateOn();
			int clsid = Cache.GetClassOfObject(hvo);
			if (hvo <= 0)
				return false;
			ICmObject owner = CmObject.CreateFromDBObject(Cache, m_cache.GetOwnerOfObject(hvo));
			int flid = m_cache.GetOwningFlidOfObject(hvo);
			if (!Cache.IsVectorProperty(flid))
				return false;

			// For example, a LexSense belonging to a LexSense can always be split off to a new
			// LexEntry.
			if (clsid == owner.ClassID)
				return true;

			// Otherwise, we need at least two vector items to be able to split off this one.
			int vectorSize = Cache.GetVectorSize(owner.Hvo, flid);
			return (vectorSize >= 2);
		}

		public virtual void HandleSplitCommand()
		{
			CheckDisposed();

			int hvo = GetObjectHvoForMenusToOperateOn();
			if (hvo <= 0)
				throw new ConfigurationException("Slice:GetObjectHvoForMenusToOperateOn is either messed up or should not have been called, because it could not find the object to be moved to a copy of its owner.", m_configurationNode);

			using (CmObjectUi ui = CmObjectUi.MakeUi(m_cache, hvo))
			{
				ui.Mediator = m_mediator;
				ui.MoveUnderlyingObjectToCopyOfOwner();
			}
			// The slice will likely be disposed in the MoveUnderlyingObjectToCopyOfOwner call,
			// so make sure we aren't collected until we leave this method, at least.
			GC.KeepAlive(this);
		}

		public virtual void HandleCopyCommand(Slice newSlice)
		{
			CheckDisposed();

			int hvoOriginal = GetObjectHvoForMenusToOperateOn();
			if (hvoOriginal <= 0)
				throw new ConfigurationException("OriginalSlice:GetObjectHvoForMenusToOperateOn is either messed up or should not have been called, because it could not find the object to be moved to a copy of its owner.", m_configurationNode);

			int hvoNew = newSlice.GetObjectHvoForMenusToOperateOn();
			if (hvoNew <= 0)
				throw new ConfigurationException("NewSlice:GetObjectHvoForMenusToOperateOn is either messed up or should not have been called, because it could not find the object to be moved to a copy of its owner.", m_configurationNode);

			ICmObject objOriginal = CmObject.CreateFromDBObject(Cache, hvoOriginal);
			ICmObject objNew = CmObject.CreateFromDBObject(Cache, hvoNew);
			if (objOriginal != null && objNew != null)
				objOriginal.CopyTo(objNew);
		}
		public virtual void HandleEditCommand()
		{
			CheckDisposed();

			// Implemented as needed by subclasses.
		}

		/// <summary>
		/// This was added for Lexical Relation slices which now have the Add/Replace Reference menu item in
		/// the dropdown menu.
		/// </summary>
		public virtual void HandleLaunchChooser()
		{
			CheckDisposed();

			// Implemented as needed by subclasses.
		}

		public virtual bool GetCanEditNow()
		{
			CheckDisposed();

			return true;
		}

		#endregion Menu Command Handlers

		#region IxCoreColleague implementation

		public virtual void Init(XCore.Mediator mediator, System.Xml.XmlNode configurationParameters)
		{
			CheckDisposed();

			m_mediator = mediator;
			m_configurationParameters = configurationParameters;
			if (Control != null && Control is IxCoreColleague)
			{
				(Control as IxCoreColleague).Init(mediator, configurationParameters);
				////				Control.AccessibilityObject.Name = this.Label;
			}
		}

		public virtual XCore.IxCoreColleague[] GetMessageTargets()
		{
			CheckDisposed();

			// Normally a slice should only handle messages if both it and its data tree
			// are visible. Override this method if there is some reason to handle messages
			// while not visible. Note however that currently (31 Aug 2005) RecordEditView
			// hides the data tree but does not remove slices when no record is current.
			// Thus, a slice that is not visible might belong to a display of a deleted
			// or unavailable object, so be very careful what you enable!
			if (this.Visible && ContainingDataTree.Visible)
			{
				if (Control != null && Control.IsDisposed)
					throw new ObjectDisposedException(ToString() + GetHashCode().ToString(), "Trying to use object that no longer exists: ");

				if (Control is IxCoreColleague)
					return new XCore.IxCoreColleague[] { Control as IxCoreColleague, this };
				else
					return new XCore.IxCoreColleague[] { this };
			}
			else
				return new XCore.IxCoreColleague[0];
		}

		#endregion IxCoreColleague implementation

		/// <summary>
		/// Updates the display of a slice, if an hvo and tag it cares about has changed in some way.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <returns>true, if it the slice updated its display</returns>
		internal protected virtual bool UpdateDisplayIfNeeded(int hvo, int tag)
		{
			CheckDisposed();

			return false;
		}

		protected void SetFieldVisibility(string visibility)
		{
			CheckDisposed();

			if (IsVisibilityItemChecked(visibility))
				return; // No change, so skip a lot of trauma.

			ReplacePartWithNewAttribute("visibility", visibility);
			DataTree dt = ContainingDataTree;
			if (!dt.ShowingAllFields)
			{
				// We remember the index of our slice, not the slice itself. Changing the visibility changes the first
				// template in the path, which makes all previous slices unreusable, so 'this' will be disposed by now.
				//int islice = this.IndexInContainer;
				dt.RefreshList(true);
				// Temporary block. It isn't selecting the right one,
				// and it ends up reorganizing the slices, if 'this' was the Pronunciation field
				// and is no longer visible.
				//if (!dt.GotoNextSliceAfterIndex(islice - 1)) // ideally select at SAME index.
				//	dt.GotoPreviousSliceBeforeIndex(islice);
			}
		}

		protected void ReplacePartWithNewAttribute(string attr, string attrValueNew)
		{
			XmlNode newPartref;
			XmlNode newLayout = Inventory.MakeOverride(
				Key,
				attr,
				attrValueNew,
				LayoutCache.LayoutVersionNumber, out newPartref);
			Inventory.GetInventory("layouts", m_cache.DatabaseName).PersistOverrideElement(newLayout);
			DataTree dt = ContainingDataTree;
			XmlNode rootKey = Key[0] as XmlNode;
			// The first item in the key is always the root XML node for the whole display. This has now changed,
			// so if we don't do something, subsequent visibility commands for other slices will use the old
			// version as a basis and lose the change we just made (unless we Refresh, which we don't want to do
			// when showing everything). Also, if we do refresh, we'll discard and remake everything.
			foreach (Slice slice in dt.Controls)
			{
				if (slice.Key != null && slice.Key.Length >= 0 && slice.Key[0] == rootKey && rootKey != newLayout)
					slice.Key[0] = newLayout;
			}

			int lastPartRef = -1;
			XmlNode oldPartRef = PartRef(out lastPartRef);
			if (oldPartRef != null)
			{
				oldPartRef = (XmlNode)Key[lastPartRef];
				Key[lastPartRef] = newPartref;

				foreach (Slice slice in dt.Controls)
				{
					if (slice.Key == null)
						continue;		// this can happen for dummy slices.  (LT-5817)
					for (int i = 0; i < slice.Key.Length; i++)
					{
						XmlNode node = slice.Key[i] as XmlNode;
						if (node == null)
							continue;

						if (XmlUtils.NodesMatch(oldPartRef, node))
							slice.Key[i] = newPartref;
					}
				}
			}
		}

		/// <summary>
		/// extract the "part ref" node from the slice.Key
		/// </summary>
		/// <returns></returns>
		protected internal XmlNode PartRef()
		{
			int indexInKey;
			return PartRef(out indexInKey);
		}

		private XmlNode PartRef(out int indexInKey)
		{
			indexInKey = -1;
			Debug.Assert(Key != null);
			if (Key == null)
				return null;
			for (int i = 0; i < Key.Length; i++)
			{
				XmlNode node = Key[i] as XmlNode;
				if (node == null || node.Name != "part" || XmlUtils.GetOptionalAttributeValue(node, "ref", null) == null)
					continue;
				indexInKey = i;
			}
			if (indexInKey != -1)
			{
				return (XmlNode)Key[indexInKey];
			}
			return null;
		}

		private void CheckVisibilityItem(UIItemDisplayProperties display, string visibility)
		{
			display.Checked = IsVisibilityItemChecked(visibility);
		}

		protected bool IsVisibilityItemChecked(string visibility)
		{
			CheckDisposed();

			XmlNode lastPartRef = null;
			foreach (object obj in Key)
			{
				XmlNode node = obj as XmlNode;
				if (node == null || node.Name != "part" || XmlUtils.GetOptionalAttributeValue(node, "ref", null) == null)
					continue;
				lastPartRef = node;
			}
			return lastPartRef != null && XmlUtils.GetOptionalAttributeValue(lastPartRef, "visibility", "always") == visibility;
		}

		public bool OnShowFieldAlwaysVisible(object args)
		{
			CheckDisposed();

			SetFieldVisibility("always");

			return true;
		}
		public bool OnShowFieldIfData(object args)
		{
			CheckDisposed();

			SetFieldVisibility("ifdata");

			return true;
		}
		public bool OnShowFieldNormallyHidden(object args)
		{
			CheckDisposed();

			SetFieldVisibility("never");

			return true;
		}

		public virtual bool OnDisplayShowFieldAlwaysVisible(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			display.Enabled = true;
			CheckVisibilityItem(display, "always");
			return true; //we've handled this
		}
		public virtual bool OnDisplayShowFieldIfData(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			display.Enabled = true;
			CheckVisibilityItem(display, "ifdata");
			return true; //we've handled this
		}
		public virtual bool OnDisplayShowFieldNormallyHidden(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			display.Enabled = true;
			CheckVisibilityItem(display, "never");
			return true; //we've handled this
		}

		/// <summary>
		/// This is used to control the width of the slice when the data tree is being laid out.
		/// Any earlier width set is meaningless.
		/// Some slices can avoid doing a lot of work by ignoring earlier OnSizeChanged messages.
		/// </summary>
		/// <param name="width"></param>
		protected internal virtual void SetWidthForDataTreeLayout(int width)
		{
			CheckDisposed();

			if (Width != width)
				Width = width;

			m_widthHasBeenSetByDataTree = true;
			SplitContainer sc = SplitCont;
			sc.SplitterMoved -= new SplitterEventHandler(mySplitterMoved);
			if (!sc.IsSplitterFixed)
				sc.SplitterMoved += new SplitterEventHandler(mySplitterMoved);
		}

		/// <summary>
		/// Note: There are two SplitterDistance event handlers on a Slice.
		/// This one handles the side effects of redrawing the tree node, when needed.
		/// Another one on DataTree takes care of updating the SplitterDisance on all the other slices.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void mySplitterMoved(object sender, SplitterEventArgs e)
		{
			SplitContainer sc = SplitCont;
			if (!sc.Panel1Collapsed)
			{
				//if ((sc.SplitterDistance > MaxAbbrevWidth && valueSansLabelindent <= MaxAbbrevWidth)
				//	|| (sc.SplitterDistance <= MaxAbbrevWidth && valueSansLabelindent > MaxAbbrevWidth))
				//{
					TreeNode.Invalidate();
				//}
			}
		}

		/// <summary>
		/// Most slices are small, this keeps initial estimates more reasonable.
		/// </summary>
		protected override Size DefaultSize
		{
			get
			{
				CheckDisposed();

				return new Size(400, 20);
			}
		}

		/// <summary>
		/// This is called when clearing the slice collection, or otherwise about to remove a slice
		/// from its parent and discard it. It allows us to put views into a state where
		/// they won't waste time if they get an OnLoad message somewhere in the course of
		/// clearing them from the collection. (LT-3118 is one problem this helped with.)
		/// </summary>
		public virtual void AboutToDiscard()
		{
			CheckDisposed();
		}
	}
}
