using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.Serialization;
using System.Windows.Forms;
using System.Xml;

using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.Resources;
using SIL.Utils;
using System.Diagnostics.CodeAnalysis;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary></summary>
	public class SliceTreeNode : UserControl, IFWDisposable
	{
		#region constants
		// Constants used in drawing tree diagram.
		// This is the width around the tree border where the mouse turns to a drag icon.
		internal const int kdxpActiveTreeBorder = 3;
		internal const int kdxpMinTreeWidth = 3; // Minimum size for tree view.
		internal const int kdxpMinDataWidth = 120; // Minimum size for data view.
		internal const int kdxpRtTreeGap = 3; // Gap between text and the right tree border.
		internal const int kdxpIconWid = 5; // Width of the minus or plus sign.
		internal const int kdzpIconGap = 1; // Width/height of gap between icon and box.
		internal const int kdypIconHeight = kdxpIconWid; // Height of plus sign.
		internal const int kdxpBoxWid = kdxpIconWid + 2 * kdzpIconGap + 2; // Width of the control box.
		internal const int kdypBoxHeight = kdxpBoxWid; // Try making the box square.
		internal const int kdxpTextGap = 2; // From line to label text.
		internal const int kdxpShortLineLen = 7; // Length of line from box to text.
		internal const int kdxpIndDist = kdxpBoxWid + kdxpShortLineLen + kdxpTextGap;
		internal const int kdypBoxCtr = kdypBoxHeight / 2; // Location for horizontal line of minus/plus/line
		internal const int kdxpBoxCtr = kdxpBoxWid / 2; // Location of vertical line of plus/line
		internal const int kdxpLongLineLen = kdxpBoxCtr + kdxpShortLineLen; // Horz. line to text w/o box.
		internal const int kdxpLeftMargin = 2; // Gap at the far left of everything.
		#endregion

		protected bool m_inMenuButton = false;

		private bool m_fShowPlusMinus = false;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		/// <summary></summary>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "parent is a reference")]
		public Slice Slice
		{
			get
			{
				CheckDisposed();

				// Depending on compile switch for SLICE_IS_SPLITCONTAINER,
				// grandParent will be both a Slice and a SplitContainer
				// (Slice is a subclass of SplitContainer),
				// or just a SplitContainer (SplitContainer is the only child Control of a Slice).
				// If grandParent is not a Slice, then we have to move up to the great-grandparent
				// to find the Slice.
				Control parent = Parent;
				while (!(parent is Slice))
					parent = parent.Parent;

				Debug.Assert(parent is Slice);

				return parent as Slice;
			}
		}

		/// <summary></summary>
		public bool ShowPlusMinus
		{
			get
			{
				CheckDisposed();
				return m_fShowPlusMinus;
			}
			set
			{
				CheckDisposed();
				m_fShowPlusMinus = value;
			}
		}

		/// <summary></summary>
		public SliceTreeNode(Slice slice)
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			SuspendLayout();
			this.Paint += new PaintEventHandler(this.HandlePaint);
			//this.MouseDown += new MouseEventHandler(this.HandleMouseDown);
			this.SizeChanged += new EventHandler(this.HandleSizeChanged);
			// Among other possible benefits, this suppresses a really nasty Heisenbug:
			// On collapsing a summary, the expansion box for the next summary was being
			// drawn without either plus or minus. This did not happen while stepping through
			// the OnPaint method, only when the program ran at full speed.
			this.SetStyle(ControlStyles.DoubleBuffer, true);
			this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			this.SetStyle(ControlStyles.UserPaint, true);
			this.TabStop = false;

			if (slice.Label != null)
				this.AccessibleName = slice.Label;
			else
				this.AccessibleName = "SliceTreeNode";
			ResumeLayout(false);
		}

		/// <summary></summary>
		public void HandleSizeChanged(object sender, EventArgs ea)
		{
			CheckDisposed();
		}

		// Things can (potentially) be dropped on this control.
		/// <summary></summary>
		public override bool AllowDrop
		{
			get
			{
				CheckDisposed();

				return true;
			}
		}

		protected override void OnDragEnter(DragEventArgs drgevent)
		{
			base.OnDragEnter(drgevent);
			int hvoDstOwner; // dummies, we just want to set the Effect property of drgevent.
			int flidDst;
			int ihvoDstStart;
			TestDropEffects(drgevent, out hvoDstOwner, out flidDst, out ihvoDstStart);
		}

		protected override void OnDragDrop(DragEventArgs drgevent)
		{
			base.OnDragDrop(drgevent);
			int hvoDstOwner;
			int flidDst;
			int ihvoDstStart;
			ObjectDragInfo odi = TestDropEffects(drgevent, out hvoDstOwner, out flidDst, out ihvoDstStart);
			if (drgevent.Effect == DragDropEffects.None)
				return;
			// Todo JohnT: verify that m_slice is the last slice in the representation of flid.
			FdoCache cache = Slice.ContainingDataTree.Cache;
			UndoableUnitOfWorkHelper.Do("Undo Move Item", "Redo Move Item",
				cache.ActionHandlerAccessor, () =>
				{
					cache.DomainDataByFlid.MoveOwnSeq(odi.HvoSrcOwner, odi.FlidSrc, odi.IhvoSrcStart,
						odi.IhvoSrcEnd, hvoDstOwner, flidDst, ihvoDstStart);
				});
		}

		/// <summary>
		/// Test whether we can drop the information indicated by the drgevent onto this.
		/// Currently this is possible if
		///		1. The drag event's data is an ObjectDragInfo
		///		2. It is valid for those objects to be moved to a position just after the object of which
		///		the destination slice is a part, in the lowest level sequence of which it is a part.
		///	drgevent.Effect is set to Move or None to indicate the result. If successful, returns an ObjectDragInfo
		///	and the place to put the new objects.
		///	(Note: the ihvoDstStart returned is meaningless if the property is a collection.)
		///
		///	Move is valid under the following conditions:
		///		1. Must not violate signature. Signature is checked if destination property is different from source.
		///		2. Must not create circularity of ownership. This is checked if destination object is different from source.
		///		3. Must result in actual movement. This is checked if destination and source object and flid are the same.
		///		This case always fails if the property is a collection, also if it is a sequence and nothing would change.
		/// </summary>
		/// <param name="drgevent">Passed so Effect can be set.</param>
		/// <param name="hvoDstOwner">Object to move things to</param>
		/// <param name="flidDst">Property to move to.</param>
		/// <param name="ihvoDstStart">Place to put them (if sequence).</param>
		private ObjectDragInfo TestDropEffects(DragEventArgs drgevent, out int hvoDstOwner, out int flidDst, out int ihvoDstStart)
		{
			ObjectDragInfo odi = (ObjectDragInfo) drgevent.Data.GetData(typeof(ObjectDragInfo));
			drgevent.Effect = DragDropEffects.None; // default
			hvoDstOwner = 0; // not used unless we get to GetSeqContext call, but compiler demands we set them.
			flidDst = 0;
			ihvoDstStart = 0;
			if (odi != null)
			{
				// Enhance JohnT: options to allow dragging onto this object, putting the dragged object into
				// one of its owning properties.
				// Try to drag the object after 'this' in the relevant property.
				if (Slice.GetSeqContext(out hvoDstOwner, out flidDst, out ihvoDstStart))
				{
					ihvoDstStart++; // Insert after the present object (if a sequence).
					if (OkToMove(hvoDstOwner, flidDst, ihvoDstStart, odi))
					{
						drgevent.Effect = DragDropEffects.Move;
						return odi;
					}
				}
				// See if the first child is a sequence we could insert at the start of.
				XmlNode firstChild = Slice.ConfigurationNode.FirstChild;
				if (firstChild != null && firstChild.Name == "seq")
				{
					hvoDstOwner = Slice.Object.Hvo;
					flidDst = (int)Slice.ContainingDataTree.Cache.DomainDataByFlid.MetaDataCache.GetFieldId2(Slice.Object.ClassID, firstChild.Attributes["field"].Value, true);
					ihvoDstStart = 0;
					if (OkToMove(hvoDstOwner, flidDst, ihvoDstStart, odi))
					{
						drgevent.Effect = DragDropEffects.Move;
						return odi;
					}
				}
			}
			return odi;
		}

		/// <summary>
		/// Return whether it is OK to move the objects indicated by odi to the specified destination.
		/// </summary>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "cache is a reference")]
		public bool OkToMove(int hvoDstOwner, int flidDst, int ihvoDstStart, ObjectDragInfo odi)
		{
			CheckDisposed();

			FDO.FdoCache cache = Slice.ContainingDataTree.Cache;
			ICmObjectRepository repo = cache.ServiceLocator.GetInstance<ICmObjectRepository>();
			if (flidDst == odi.FlidSrc)
			{
				// Verify that it is not a no-operation.
				if (hvoDstOwner == odi.HvoSrcOwner)
				{
					// Same property of same object. If it's a collection, disable.
					CellarPropertyType fieldType = (CellarPropertyType)cache.DomainDataByFlid.MetaDataCache.GetFieldType((int)flidDst);
					// We can't drag it to the position it's already at; that's no change. We also can't drag it
					// to the position one greater: that amounts to trying to place it after itself, which (after
					// removing it from before itself) amounts to a no-operation.
					if (fieldType == CellarPropertyType.OwningSequence &&
						ihvoDstStart != odi.IhvoSrcStart && ihvoDstStart != odi.IhvoSrcStart + 1)
					{
						// It's a sequence and the target and source positions are different, so we can do it.
						return true;
					}
				}
				else
				{
					// Different objects; need to verify no circular ownership involved.
					for (int ihvo = odi.IhvoSrcStart; ihvo <= odi.IhvoSrcEnd; ihvo++)
					{
						int hvo = cache.DomainDataByFlid.get_VecItem(odi.HvoSrcOwner, odi.FlidSrc, ihvo);
						// See if hvoDstOwner is owned by hvo
						ICmObject obj2 = repo.GetObject(hvoDstOwner);
						// loop from hvo2 to root owner of hvo2. If hvo2 or any of its owners is hvo,
						// we have a problem.
						while (obj2 != null)
						{
							if (hvo == obj2.Hvo)
								return false; // circular ownership, can't drop.
							obj2 = obj2.Owner;
						}
					}
					return true;
				}
			}
			else
			{
				// Different property, check signature.
				IFwMetaDataCache mdc = cache.DomainDataByFlid.MetaDataCache;
				int luclid = mdc.GetDstClsId((int) flidDst);
				for (int ihvo = odi.IhvoSrcStart; ihvo <= odi.IhvoSrcEnd; ihvo++)
				{
					int hvo = cache.DomainDataByFlid.get_VecItem(odi.HvoSrcOwner, odi.FlidSrc, ihvo);
					int cls = repo.GetObject(hvo).ClassID;
					while (cls != 0 && cls != luclid)
					{
						cls = mdc.GetBaseClsId(cls);
					}
					if (cls == 0)
						return false; // wrong signature, can't drop.
				}
				// All sigs OK, allow drop.
				return true;
			}
			// If none of those cases is OK, can't do it.
			return false;
		}

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

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if( disposing )
			{
				this.Paint -= new PaintEventHandler(this.HandlePaint);
				this.SizeChanged -= new EventHandler(this.HandleSizeChanged);
				this.KeyPress -= new System.Windows.Forms.KeyPressEventHandler(this.SliceTreeNode_KeyPress);
				this.KeyDown -= new System.Windows.Forms.KeyEventHandler(this.SliceTreeNode_KeyDown);
				if(components != null)
				{
					components.Dispose();
				}
			}

			base.Dispose( disposing );
		}

		void HandlePaint(object sender, PaintEventArgs pea)
		{
			// Do nothing if invisible. Don't know why the framework even
			// calls us if invisible, but it does, and if we call NextFieldAtIndent
			// that generates more slices, which get drawn invisibly in turn, and
			// laziness is defeated completely.
			if (pea.ClipRectangle.Height == 0 || pea.ClipRectangle.Width == 0)
				return;

			if (Slice.Parent == null)
				// FWNX-436
				return;

			Graphics gr = pea.Graphics;

			Color lineColor = Color.FromKnownColor(KnownColor.ControlDark);
			using (Pen linePen = new Pen(lineColor, 1))
			{
			linePen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
				using (Pen boxLinePen = new Pen(lineColor, 1))
				using (Brush backgroundBrush = new SolidBrush(Slice.ContainingDataTree.BackColor))
				using (Brush lineBrush = new SolidBrush(lineColor))
				{
			int nIndent = Slice.Indent;
			DataTree.TreeItemState tis = Slice.Expansion;
			// Drawing within a control that covers the tree node portion of this slice, we always
			// draw relative to a top-of-slice that is 0. I'm keeping the variable just in case
			// we ever go back to drawing in the parent window.
			int ypTopOfSlice = 0;
			// int ypTopOfNextSlice = this.Height; // CS2019
			int iSlice = Slice.ContainingDataTree.Slices.IndexOf(Slice);
			// Go through the indents. This used to draw the correct tree structure at each level.
			// Now we leave out the structue, but this figures out some stuff we need if we end up
			// drawing a box. This could be optimized if we really never want the tree diagram.
			for (int nInd = 0; nInd <= nIndent; ++nInd)
			{
				// int ypTreeTop = ypTopOfSlice; // CS2019
				int xpBoxLeft = kdxpLeftMargin + nInd * kdxpIndDist;
				int xpBoxCtr = xpBoxLeft + kdxpBoxCtr;
				// Enhance JohnT: 2nd argument of max should be label height.
				int dypBranchHeight = Slice.GetBranchHeight();
				int dypLeftOver = Math.Max(kdypBoxHeight / 2, dypBranchHeight) - kdypBoxHeight / 2;
				int ypBoxTop = ypTopOfSlice + dypLeftOver;
				int ypBoxCtr = ypBoxTop + kdypBoxHeight / 2;
				// int xpRtLineEnd = xpBoxCtr + kdxpLongLineLen; // CS2019

				// There are two possible locations for the start and stop points for the
				// vertical line. That will produce three different results which I have
				// attempted to illustrate below. In case that's unclear they are:
				// an L - shaped right angle, a T - shape rotated counter-clockwise by
				// 90 degrees and an inverted L shape (i.e. flipped vertically).
				//
				// |_  > ypStart = top of field, ypStop = center point of +/- box.
				// |-  > ypStart = top of field, ypStop = bottom of field.
				// |  > ypStart = center point of +/- box, ypStop = bottom of field.
				//
				// Draw the vertical line.
				bool fMoreFieldsAtLevel = (Slice.ContainingDataTree.NextFieldAtIndent(nInd, iSlice) != 0);

				// Process a terminal level with a box.
				if (ShowPlusMinus && nInd == nIndent && tis != DataTree.TreeItemState.ktisFixed)
				{
					// Draw the box.
					Rectangle rcBox = new Rectangle(xpBoxLeft, ypBoxTop, kdxpBoxWid, kdypBoxHeight);
					gr.FillRectangle(lineBrush, rcBox);
					// Erase the inside of the box as we may have drawn dotted lines there.
					rcBox.Inflate(-1, -1);
					gr.FillRectangle(backgroundBrush, rcBox);

					if (tis != DataTree.TreeItemState.ktisCollapsedEmpty)
					{
						// Draw the minus sign.
						int xpLeftMinus = xpBoxLeft + 1 + kdzpIconGap;
						gr.DrawLine(boxLinePen, xpLeftMinus, ypBoxCtr, xpLeftMinus + kdxpIconWid - 1, ypBoxCtr);

						if (tis == DataTree.TreeItemState.ktisCollapsed)
						{
							// Draw the vertical part of the plus, if we are collapsed.
							int ypTopPlus = ypBoxTop + 1 + kdzpIconGap;
							gr.DrawLine(boxLinePen, xpBoxCtr, ypTopPlus, xpBoxCtr, ypTopPlus + kdypIconHeight - 1);
						}
					}
				}
			}

			//			// If the height of the slice is greater then one line (1.5 * LabelHeight) and
			//			// the slice has a child, then we need to draw a line to that child. (fixes a
			//			// gap that appears otherwise)
			//			int left = kdxpLeftMargin + (nIndent + 1) * kdxpIndDist;
			//			int center = left + kdxpBoxCtr;
			//			bool fHasChildren = (m_slice.Diagram.NextFieldAtIndent(nIndent + 1, iSlice) != 0);
			//			if (fHasChildren && Height > m_slice.LabelHeight * 1.5)
			//			{
			//				gr.DrawLine(linePen, center, ypTopOfSlice + m_slice.LabelHeight,
			//					center, ypTopOfNextSlice);
			//			}

			if (ShowingContextIcon)
			{
				// Show context menu icon
				gr.DrawImage(ResourceHelper.BlueCircleDownArrow, 2, 1);
			}

			//			int xIndent = m_slice.LabelIndent();
			//			int lineWidth = 1;
			//			Slice nextSlice = m_slice.ContainingDataTree.Slices[m_slice.IndexInContainer + 1] as Slice;
			//			int yPos = this.Height - 1;
			//			if (nextSlice.Weight == ObjectWeight.heavy)
			//			{
			//				lineWidth += DataTree.HeavyweightObjectExtra;
			//				//yPos -= DataTree.HeavyweightObjectExtra / 2;
			//			}
			//			Pen borderPen = new Pen(Color.LightGray, lineWidth);
			//			gr.DrawLine(borderPen, xIndent, yPos, this.Width, yPos);

			Slice.DrawLabel(ypTopOfSlice, gr, pea.ClipRectangle.Width);
		}
			}
		}

		private bool ShowingContextIcon
		{
			get
			{
				return !ShowPlusMinus && Slice.ShowContextMenuIconInTreeNode();
			}
		}

		/// <summary>
		/// Double-click causes expand/contract wherever it is.
		/// </summary>
		protected override void OnDoubleClick(EventArgs e)
		{
			base.OnDoubleClick (e);
			if (Slice.Expansion != DataTree.TreeItemState.ktisFixed)
			{
				int iSlice = Slice.ContainingDataTree.Slices.IndexOf(Slice);
				ToggleExpansionAndScroll(iSlice);
			}
		}

		/// <summary>
		/// Preliminary version of mouse down handles expansion and contraction.
		/// Todo:
		///		- Scroll appropriately to show as much as possible of what was expanded.
		///		- drag and drop effects.
		///		- double-click on label also toggles expansion.
		/// </summary>
		protected override void OnMouseDown(MouseEventArgs meArgs)
		{
			//in light of what JT says below, we need to explicitly tell the slice that
			//we were clicked on, because we cannot count on a normal Click event
			//which we would normally just subscribe to, as we do with the slice editor controls.
			Slice.OnTreeNodeClick(this, meArgs);

			// The documentation says we should call the base class. Not doing so means that
			// mouse down handlers can't be attached to this class by delegation.
			// However, the base class implementation has a catastrophic side effect: it causes
			// this control to be selected, in a sense that causes it to be scrolled into
			// view every time a lazy slice is expanded into a real one. This is the first
			// successful way I (JT) found to defeat this behavior, and I tried many.
			//base.OnMouseDown(meArgs);
			if (meArgs.Button.Equals(MouseButtons.Right) || (ShowingContextIcon && meArgs.X < 20))
			{
				//begin test (JDH)
				Point p = new Point(meArgs.X,meArgs.Y);
				if (Slice.HandleMouseDown(p))
				{
					return;
				}
				//end test
			}

			// Enhance JohnT: Could we find a better label that shows more clearly what is being moved?
			int hvoSrcOwner;
			int flidSrc;
			int ihvoSrcStart;
			if (!Slice.GetSeqContext(out hvoSrcOwner, out flidSrc, out ihvoSrcStart))
				return; // If we can't identify an object to move, don't do a drag.
			ObjectDragInfo objinfo = new ObjectDragInfo(hvoSrcOwner, flidSrc, ihvoSrcStart, ihvoSrcStart, Slice.Label);
			DataObject dataobj = new DataObject(objinfo);
			// Initiate a drag/drop operation. Currently we only support move.
			// Enhance JohnT: Also support Copy.
			DoDragDrop(dataobj, DragDropEffects.Move);
		}

		/// <summary>
		/// Toggle the expansion state of the slice at iSlice. May presume it is already in existence.
		/// Todo: attempt to scroll so new children are visible
		/// </summary>
		public void ToggleExpansionAndScroll(int iSlice)
		{
			CheckDisposed();

			ToggleExpansion(iSlice);
		}

		/// <summary>
		/// Toggle the expansion state of the slice at iSlice. May presume it is already in existence.
		/// (If it is in the collapsedEmpty or fixed states, do nothing.)
		/// </summary>
		public void ToggleExpansion(int iSlice)
		{
			CheckDisposed();

			// Why don't we just let the slice do all the toggle work?
			if (Slice.Expansion == DataTree.TreeItemState.ktisCollapsed)
			{
				// expand it
				Slice.Expand(iSlice);
			}
			else if (Slice.Expansion == DataTree.TreeItemState.ktisExpanded)
			{
				// collapse it
				Slice.Collapse(iSlice);
			}
			else
			{
				// Either collapsedEmpty, or the user happened to click in the area
				// that the box would occupy in a fixed node...in either case, nothing to do.
			}
		}

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SliceTreeNode));
			this.SuspendLayout();
			//
			// SliceTreeNode
			//
			this.Name = "SliceTreeNode";
			resources.ApplyResources(this, "$this");
			this.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.SliceTreeNode_KeyPress);
			this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.SliceTreeNode_KeyDown);
			this.ResumeLayout(false);

		}
		#endregion

		//!!!this is never called  (arrrgggghhh)
		private void SliceTreeNode_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
		{
			Debug.WriteLine("TreeNode key press");
		}

		//!!!this is never called  (arrrgggghhh)
		private void SliceTreeNode_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			Debug.WriteLine("TreeNode key down");
		}

		#if __MonoCS__
		/// <summary>
		/// Activate menu only if Alt key is being pressed.  See FWNX-1353.
		/// </summary>
		/// <remarks>TODO: Getting here without the Alt key may be considered a Mono bug.</remarks>
		protected override bool ProcessDialogChar(char charCode)
		{
			if (Control.ModifierKeys == Keys.Alt)
				return base.ProcessDialogChar(charCode);
			return false;
		}
		#endif
	}

	/// <summary></summary>
	[Serializable()]
	public class ObjectDragInfo : ISerializable
	{
		int m_hvoSrcOwner;
		int m_flidSrc;
		int m_ihvoSrcStart;
		int m_ihvoSrcEnd;
		string m_label; // The label to display during dragging.

		/// <summary></summary>
		public ObjectDragInfo(int hvoSrcOwner, int flidSrc, int ihvoSrcStart, int ihvoSrcEnd, string label)
		{
			m_hvoSrcOwner = hvoSrcOwner;
			m_flidSrc = flidSrc;
			m_ihvoSrcStart = ihvoSrcStart;
			m_ihvoSrcEnd = ihvoSrcEnd;
			m_label = label;
		}

		/// <summary></summary>
		public override string ToString()
		{
			return m_label;
		}

		/// <summary>Deserialization constructor.</summary>
		public ObjectDragInfo (SerializationInfo info, StreamingContext context)
		{
			m_hvoSrcOwner = (int)info.GetValue("SrcOwner", typeof(int));
			m_flidSrc = (int)info.GetValue("FlidSrc", typeof(int));
			m_ihvoSrcStart = (int)info.GetValue("IhvoSrcStart", typeof(int));
			m_ihvoSrcEnd = (int)info.GetValue("IhvoSrcEnd", typeof(int));
			m_label = (String)info.GetValue("label", typeof(string));
		}

		/// <summary>Serialization function.</summary>
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("SrcOwner", m_hvoSrcOwner);
			info.AddValue("FlidSrc", m_flidSrc);
			info.AddValue("IhvoSrcStart", m_ihvoSrcStart);
			info.AddValue("IhvoSrcEnd", m_ihvoSrcEnd);
			info.AddValue("label", m_label);
		}

		/// <summary></summary>
		public int HvoSrcOwner
		{
			get
			{
				return m_hvoSrcOwner;
			}
		}

		/// <summary></summary>
		public int FlidSrc
		{
			get
			{
				return m_flidSrc;
			}
		}

		/// <summary></summary>
		public int IhvoSrcStart
		{
			get
			{
				return m_ihvoSrcStart;
			}
		}

		/// <summary></summary>
		public int IhvoSrcEnd
		{
			get
			{
				return m_ihvoSrcEnd;
			}
		}
	}
}
