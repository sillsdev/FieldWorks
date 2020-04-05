// Copyright (c) 2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.Remoting;
using System.Windows.Forms;
using System.Windows.Forms.Layout;
using System.Xml.Linq;
using LanguageExplorer.Controls.DetailControls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorer
{
	internal interface ISlice : IFlexComponent
	{
		ISlice NextSlice { get; }

		UserControl AsUserControl { get; }

		/// <summary>
		/// The weight of object that starts at the beginning of this slice.
		/// </summary>
		ObjectWeight Weight { get; set; }

		ContextMenuName HotlinksMenuId { get; }

		/// <summary />
		object[] Key { get; set; }

		/// <summary />
		IPersistenceProvider PersistenceProvider { get; set; }

		/// <summary />
		DataTree ContainingDataTree { get; }

		SplitContainer SplitCont { get; }

		/// <summary />
		SliceTreeNode TreeNode { get; }

		/// <summary />
		LcmCache Cache { get; set; }

		/// <summary />
		ICmObject MyCmObject { get; set; }

		/// <summary>
		/// the XElement that was used to construct this slice
		/// </summary>
		XElement ConfigurationNode { get; set; }

		/// <summary>
		/// This element stores the caller for future processing
		/// </summary>
		XElement CallerNode { get; set; }

		/// <summary />
		Control Control { get; set; }

		/// <summary>
		/// is this node representing a property which is an (ordered) sequence?
		/// </summary>
		bool IsSequenceNode { get; }

		/// <summary>
		/// is this node a header?
		/// </summary>
		bool IsHeaderNode { get; }

		/// <summary>
		/// In some contexts we insert into the slice array a 'dummy' slice
		/// which can handle some queries directly (e.g., it may know
		/// its indentation level) but needs to 'BecomeReal' if it becomes
		/// fully visible. The purpose is laziness...often we insert the
		/// same dummy slice into many locations, and they are progressively
		/// replaced with real ones.
		/// </summary>
		bool IsRealSlice { get; }

		/// <summary>
		/// In some contexts, we use a "ghost" slice to represent data that
		/// has not yet been created.  These are "real" slices, but they don't
		/// represent "real" data.  Thus, for example, the underlying object
		/// can't be deleted because it doesn't exist.  (But the ghost slice
		/// may claim to have an object, because it needs such information to
		/// create the data once the user decides to type something...
		/// </summary>
		bool IsGhostSlice { get; }

		/// <summary>
		/// Determines how deeply indented this item is in the tree diagram. 0 means no indent.
		/// </summary>
		int Indent { get; set; }

		/// <summary>
		/// Return the expansion state of tree nodes.
		/// </summary>
		/// <returns>A tree state enum.</returns>
		TreeItemState Expansion { get; set; }

		/// <summary>
		/// Gets and sets the label used to identify the item in the tree diagram.
		/// May need to override if not using the standard variable to draw a simple label
		/// </summary>
		string Label { get; set; }

		/// <summary />
		string Abbreviation { get; set; }

		/// <summary>
		/// Text to display as tooltip for label (SliceTreeNode).
		/// Defaults to Label.
		/// </summary>
		string ToolTip { get; }

		/// <summary>
		/// Help Topic ID for the slice
		/// </summary>
		string HelpTopicID { get; }

		/// <summary />
		int IndexInContainer { get; }

		/// <summary>
		/// Record the slice that 'owns' this one (typically this was created by a CreateIndentedNodes call on the
		/// parent slice).
		/// </summary>
		ISlice ParentSlice { get; set; }

		/// <summary>
		/// Get the flid associated with this slice, if there is one.
		/// </summary>
		int Flid { get; }

		/// <summary>
		/// is it possible to do a deletion menu command on this slice right now?
		/// </summary>
		bool CanDeleteNow { get; }

		/// <summary>
		/// Is it possible to do a merge menu command on this slice right now?
		/// </summary>
		bool CanMergeNow { get; }

		/// <summary>
		/// Is it possible to do a split menu command on this slice right now?
		/// </summary>
		bool CanSplitNow { get; }
		/// <summary />
		bool CanEditNow { get; }
		bool AutoSize { get; set; }
		AutoSizeMode AutoSizeMode { get; set; }
		AutoValidate AutoValidate { get; set; }
		BorderStyle BorderStyle { get; set; }
		string Text { get; set; }
		SizeF AutoScaleDimensions { get; set; }
		AutoScaleMode AutoScaleMode { get; set; }
		BindingContext BindingContext { get; set; }
		Control ActiveControl { get; set; }
		SizeF CurrentAutoScaleDimensions { get; }
		Form ParentForm { get; }
		bool AutoScroll { get; set; }
		Size AutoScrollMargin { get; set; }
		Point AutoScrollPosition { get; set; }
		Size AutoScrollMinSize { get; set; }
		Rectangle DisplayRectangle { get; }
		HScrollProperties HorizontalScroll { get; }
		VScrollProperties VerticalScroll { get; }
		ScrollableControl.DockPaddingEdges DockPadding { get; }
		AccessibleObject AccessibilityObject { get; }
		string AccessibleDefaultActionDescription { get; set; }
		string AccessibleDescription { get; set; }
		string AccessibleName { get; set; }
		AccessibleRole AccessibleRole { get; set; }
		bool AllowDrop { get; set; }
		AnchorStyles Anchor { get; set; }
		Point AutoScrollOffset { get; set; }
		LayoutEngine LayoutEngine { get; }
		Color BackColor { get; set; }
		Image BackgroundImage { get; set; }
		ImageLayout BackgroundImageLayout { get; set; }
		int Bottom { get; }
		Rectangle Bounds { get; set; }
		bool CanFocus { get; }
		bool CanSelect { get; }
		bool Capture { get; set; }
		bool CausesValidation { get; set; }
		Rectangle ClientRectangle { get; }
		Size ClientSize { get; set; }
		string CompanyName { get; }
		bool ContainsFocus { get; }
		ContextMenu ContextMenu { get; set; }
		ContextMenuStrip ContextMenuStrip { get; set; }
		Control.ControlCollection Controls { get; }
		bool Created { get; }
		Cursor Cursor { get; set; }
		ControlBindingsCollection DataBindings { get; }
		bool IsDisposed { get; }
		bool Disposing { get; }
		DockStyle Dock { get; set; }
		bool Enabled { get; set; }
		bool Focused { get; }
		Font Font { get; set; }
		Color ForeColor { get; set; }
		IntPtr Handle { get; }
		bool HasChildren { get; }
		int Height { get; set; }
		bool IsHandleCreated { get; }
		bool InvokeRequired { get; }
		bool IsAccessible { get; set; }
		bool IsMirrored { get; }
		int Left { get; set; }
		Point Location { get; set; }
		Padding Margin { get; set; }
		Size MaximumSize { get; set; }
		Size MinimumSize { get; set; }
		string Name { get; set; }
		Control Parent { get; set; }
		string ProductName { get; }
		string ProductVersion { get; }
		bool RecreatingHandle { get; }
		Region Region { get; set; }
		int Right { get; }
		RightToLeft RightToLeft { get; set; }
		ISite Site { get; set; }
		Size Size { get; set; }
		int TabIndex { get; set; }
		bool TabStop { get; set; }
		object Tag { get; set; }
		int Top { get; set; }
		Control TopLevelControl { get; }
		bool UseWaitCursor { get; set; }
		bool Visible { get; set; }
		int Width { get; set; }
		IWindowTarget WindowTarget { get; set; }
		Size PreferredSize { get; }
		Padding Padding { get; set; }
		ImeMode ImeMode { get; set; }
		IContainer Container { get; }

		void RemoveOldVisibilityMenus();
		int GetSelectionHvoFromControls();

		/// <summary>
		/// Add these menus:
		/// 1. Separator (but only if there are already items in the ContextMenuStrip).
		/// 2. 'Field Visibility', and its three sub-menus.
		/// 3. Have Slice subclasses to add ones they need (e.g., Writing Systems and its sub-menus).
		/// 4. 'Help...'
		/// </summary>
		void AddCoreContextMenus(ref Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> sliceTreeNodeContextMenuStripTuple);

		void PrepareToShowContextMenu();

		/// <summary>
		/// This method should be called once the various properties of the slice have been set,
		/// particularly the Cache, Object, Key, and Spec. The slice may create its Control in
		/// this method, so don't assume it exists before this is called. It should be called
		/// before installing the slice.
		/// </summary>
		void FinishInit();

		/// <summary>
		/// This is passed the color that the XDE specified, if any, otherwise null.
		/// The default is to use the normal window color for editable text.
		/// Subclasses which know they should have a different default should
		/// override this method, but normally should use the specified color if not
		/// null.
		/// </summary>
		void OverrideBackColor(string backColorName);

		/// <summary>
		/// We tend to get a visual stuttering effect if sub-controls are made visible before the
		/// main slice is correctly positioned. This method is called after the slice is positioned
		/// to give it a chance to make embedded controls visible.
		/// This default implementation does nothing.
		/// </summary>
		void ShowSubControls();

		/// <summary>
		/// The slice should become the focus slice (and return true).
		/// If the fOkToFocusTreeNode argument is false, this should happen iff it has a control which
		/// is appropriate to focus.
		/// Note: JohnT: recently I noticed that trying to focus the tree node doesn't seem to do
		/// anything; I'm not sure passing true is useful.
		/// </summary>
		bool TakeFocus(bool fOkToFocusTreeNode);

		/// <summary>
		/// Some 'unreal' slices can become 'real' (ready to actually display) without
		/// actually replacing themselves with a different object. Such slices override
		/// this method to do whatever is needed and then answer true. If a slice
		/// answers false to IsRealSlice, this is tried, and if it returns false,
		/// then BecomeReal is called.
		/// </summary>
		bool BecomeRealInPlace();

		/// <summary>
		/// In some contexts we insert into the slice array
		/// </summary>
		void BecomeReal(int index);

		/// <summary />
		bool ShowContextMenuIconInTreeNode();

		/// <summary />
		void SetCurrentState(bool isCurrent);

		/// <summary />
		void Install(DataTree parentDataTree);

		/// <summary>
		/// Attempt to set the split position, but do NOT modify the global setting for
		/// the data tree if unsuccessful. This occurs during window initialization, since
		/// (I think) slices are created before the proper width is set for the containing
		/// data pane, and the constraints on the width of the splitter may not allow it to
		/// take on the persisted position.
		/// </summary>
		void SetSplitPosition();

		/// <summary />
		void GenerateChildren(XElement node, XElement caller, ICmObject obj, int indent, ref int insPos, ArrayList path, ObjSeqHashMap reuseMap, bool fUsePersistentExpansion);

		/// <summary />
		string GetChooserHelpTopicID();

		string GetChooserHelpTopicID(string chooserDlgHelpTopicID);

		/// <summary>
		/// Returns the height, from the top of the item, at which to draw the line across towards it.
		/// Typically this is the center of where DrawLabel will draw the label, but it might not be (e.g.,
		/// if DrawLabel actually draws two labels and a bit of tree diagram).
		/// </summary>
		int GetBranchHeight();

		/// <summary />
		void Expand();

		/// <summary>
		/// Expand this node, which is at position iSlice in its parent.
		/// </summary>
		/// <remarks> I (JH) don't know why this was written to take the index of the slice.
		/// It's just as easy for this class to find its own index.
		/// JohnT: for performance; finding its own index is a linear search,
		/// and the caller often has the info already, especially in loops expanding many children.</remarks>
		void Expand(int iSlice);

		/// <summary />
		void Collapse();

		/// <summary>
		/// Collapse this node, which is at position iSlice in its parent.
		/// </summary>
		void Collapse(int iSlice);

		/// <summary />
		bool HandleMouseDown(Point p);

		/// <summary />
		int LabelIndent();

		/// <summary>
		/// Draws the label in the containing SilTreeControl's Graphics object at the specified position.
		/// Override if you have a more complex type of label, e.g., if the field contains interlinear
		/// data and you want to label each line.
		/// </summary>
		void DrawLabel(int y, Graphics gr, int clipWidth);

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
		bool GetSeqContext(out int hvoOwner, out int flid, out int ihvoPosition);

		/// <summary>
		/// do an insertion
		/// </summary>
		/// <param name="fieldName">name of field to create in</param>
		/// <param name="className">class of object to create</param>
		/// <param name="ownerClassName">class of expected owner. If the current slice's object is not
		/// this class (or a subclass), look for a containing object that is.</param>
		/// <remarks> called by the containing environment in response to a user command.</remarks>
		void HandleInsertCommand(string fieldName, string className, string ownerClassName = null);

		/// <summary>
		/// Main work of deleting an object; answer true if it was actually deleted.
		/// </summary>
		bool HandleDeleteCommand();

		/// <summary>
		/// Build a list of slices close to the current one, ordered by distance,
		/// starting with the slice itself. An arbitrary maximum distance (currently 40) is imposed,
		/// to minimize the time spent getting and using these; usually one of the first few is used.
		/// </summary>
		List<ISlice> GetNearbySlices();

		/// <summary>
		/// Gives the object that should be the target of Delete, copy, etc. for menus operating on this slice label.
		/// </summary>
		/// <returns>Return null if this slice is supposed to operate on an atomic field which is currently empty.</returns>
		ICmObject GetObjectForMenusToOperateOn();

		/// <summary />
		void HandleMergeCommand(bool fLoseNoTextData);

		/// <summary />
		void HandleSplitCommand();

		/// <summary />
		void HandleEditCommand();

		/// <summary>
		/// This was added for Lexical Relation slices which now have the Add/Replace Reference menu item in
		/// the dropdown menu.
		/// </summary>
		void HandleLaunchChooser();

		/// <summary>
		/// Updates the display of a slice, if an hvo and tag it cares about has changed in some way.
		/// </summary>
		/// <returns>true, if it the slice updated its display</returns>
		bool UpdateDisplayIfNeeded(int hvo, int tag);

		/// <summary>
		/// This is used to control the width of the slice when the data tree is being laid out.
		/// Any earlier width set is meaningless.
		/// Some slices can avoid doing a lot of work by ignoring earlier OnSizeChanged messages.
		/// </summary>
		void SetWidthForDataTreeLayout(int width);

		/// <summary>
		/// This is called when clearing the slice collection, or otherwise about to remove a slice
		/// from its parent and discard it. It allows us to put views into a state where
		/// they won't waste time if they get an OnLoad message somewhere in the course of
		/// clearing them from the collection. (LT-3118 is one problem this helped with.)
		/// </summary>
		void AboutToDiscard();
		bool ValidateChildren();
		bool ValidateChildren(ValidationConstraints validationConstraints);
		event EventHandler AutoSizeChanged;
		event EventHandler AutoValidateChanged;
		event EventHandler Load;
		event EventHandler TextChanged;
		void PerformAutoScale();
		bool Validate();
		bool Validate(bool checkAutoValidate);
		void ScrollControlIntoView(Control activeControl);
		void SetAutoScrollMargin(int x, int y);
		event ScrollEventHandler Scroll;
		void ResetBindings();
		Size GetPreferredSize(Size proposedSize);
		IAsyncResult BeginInvoke(Delegate method);
		IAsyncResult BeginInvoke(Delegate method, params object[] args);
		void BringToFront();
		bool Contains(Control ctl);
		Graphics CreateGraphics();
		void CreateControl();
		DragDropEffects DoDragDrop(object data, DragDropEffects allowedEffects);
		void DrawToBitmap(Bitmap bitmap, Rectangle targetBounds);
		object EndInvoke(IAsyncResult asyncResult);
		Form FindForm();
		bool Focus();
		Control GetChildAtPoint(Point pt, GetChildAtPointSkip skipValue);
		Control GetChildAtPoint(Point pt);
		IContainerControl GetContainerControl();
		Control GetNextControl(Control ctl, bool forward);
		void Hide();
		void Invalidate(Region region);
		void Invalidate(Region region, bool invalidateChildren);
		void Invalidate();
		void Invalidate(bool invalidateChildren);
		void Invalidate(Rectangle rc);
		void Invalidate(Rectangle rc, bool invalidateChildren);
		object Invoke(Delegate method);
		object Invoke(Delegate method, params object[] args);
		void PerformLayout();
		void PerformLayout(Control affectedControl, string affectedProperty);
		Point PointToClient(Point p);
		Point PointToScreen(Point p);
		bool PreProcessMessage(ref Message msg);
		PreProcessControlState PreProcessControlMessage(ref Message msg);
		void ResetBackColor();
		void ResetCursor();
		void ResetFont();
		void ResetForeColor();
		void ResetRightToLeft();
		Rectangle RectangleToClient(Rectangle r);
		Rectangle RectangleToScreen(Rectangle r);
		void Refresh();
		void ResetText();
		void ResumeLayout();
		void ResumeLayout(bool performLayout);
		void Scale(float ratio);
		void Scale(float dx, float dy);
		void Scale(SizeF factor);
		void Select();
		bool SelectNextControl(Control ctl, bool forward, bool tabStopOnly, bool nested, bool wrap);
		void SendToBack();
		void SetBounds(int x, int y, int width, int height);
		void SetBounds(int x, int y, int width, int height, BoundsSpecified specified);
		void Show();
		void SuspendLayout();
		void Update();
		void ResetImeMode();
		event EventHandler BackColorChanged;
		event EventHandler BackgroundImageChanged;
		event EventHandler BackgroundImageLayoutChanged;
		event EventHandler BindingContextChanged;
		event EventHandler CausesValidationChanged;
		event EventHandler ClientSizeChanged;
		event EventHandler ContextMenuChanged;
		event EventHandler ContextMenuStripChanged;
		event EventHandler CursorChanged;
		event EventHandler DockChanged;
		event EventHandler EnabledChanged;
		event EventHandler FontChanged;
		event EventHandler ForeColorChanged;
		event EventHandler LocationChanged;
		event EventHandler MarginChanged;
		event EventHandler RegionChanged;
		event EventHandler RightToLeftChanged;
		event EventHandler SizeChanged;
		event EventHandler TabIndexChanged;
		event EventHandler TabStopChanged;
		event EventHandler VisibleChanged;
		event EventHandler Click;
		event ControlEventHandler ControlAdded;
		event ControlEventHandler ControlRemoved;
		event DragEventHandler DragDrop;
		event DragEventHandler DragEnter;
		event DragEventHandler DragOver;
		event EventHandler DragLeave;
		event GiveFeedbackEventHandler GiveFeedback;
		event EventHandler HandleCreated;
		event EventHandler HandleDestroyed;
		event HelpEventHandler HelpRequested;
		event InvalidateEventHandler Invalidated;
		event EventHandler PaddingChanged;
		event PaintEventHandler Paint;
		event QueryContinueDragEventHandler QueryContinueDrag;
		event QueryAccessibilityHelpEventHandler QueryAccessibilityHelp;
		event EventHandler DoubleClick;
		event EventHandler Enter;
		event EventHandler GotFocus;
		event KeyEventHandler KeyDown;
		event KeyPressEventHandler KeyPress;
		event KeyEventHandler KeyUp;
		event LayoutEventHandler Layout;
		event EventHandler Leave;
		event EventHandler LostFocus;
		event MouseEventHandler MouseClick;
		event MouseEventHandler MouseDoubleClick;
		event EventHandler MouseCaptureChanged;
		event MouseEventHandler MouseDown;
		event EventHandler MouseEnter;
		event EventHandler MouseLeave;
		event EventHandler MouseHover;
		event MouseEventHandler MouseMove;
		event MouseEventHandler MouseUp;
		event MouseEventHandler MouseWheel;
		event EventHandler Move;
		event PreviewKeyDownEventHandler PreviewKeyDown;
		event EventHandler Resize;
		event UICuesEventHandler ChangeUICues;
		event EventHandler StyleChanged;
		event EventHandler SystemColorsChanged;
		event CancelEventHandler Validating;
		event EventHandler Validated;
		event EventHandler ParentChanged;
		event EventHandler ImeModeChanged;
		void Dispose();
		string ToString();
		event EventHandler Disposed;
		object GetLifetimeService();
		object InitializeLifetimeService();
		ObjRef CreateObjRef(Type requestedType);
	}
}