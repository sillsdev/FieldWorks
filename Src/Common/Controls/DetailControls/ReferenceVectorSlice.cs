// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ReferenceCollectionSlice.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
//	These slices are useful for collections andsequences.  No re-ordering is not supported
//	for sequences. Also, adding the same element multiple times is not allowed for collections,
//	but it is for sequences.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Windows.Forms;

using SIL.Utils;
using SIL.FieldWorks.FDO;
using XCore;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// Class that can edit a field that is a sequence of object references, typically by launching a chooser.
	/// Also a base class for more specialized editors.
	/// Control should be a (subclass of) VectorReferenceLauncher.
	/// </summary>
	public class ReferenceVectorSlice : ReferenceSlice
	{
		int m_dxLastWidth; // remember width when OnSizeChanged called.

		/// <summary>
		/// Initializes a new instance of the <see cref="ReferenceVectorSlice"/> class.
		/// Used by custom slices that extend this class.
		/// </summary>
		/// <param name="control">The control.</param>
		protected ReferenceVectorSlice(Control control)
			: base(control)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ReferenceVectorSlice"/> class.
		/// Used by slices that extend this class.
		/// </summary>
		/// <param name="control">The control.</param>
		/// <param name="cache">The cache.</param>
		/// <param name="obj">The obj.</param>
		/// <param name="flid">The flid.</param>
		protected ReferenceVectorSlice(Control control, FdoCache cache, ICmObject obj, int flid)
			: base(control, cache, obj, flid)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ReferenceVectorSlice"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="obj">The obj.</param>
		/// <param name="flid">The flid.</param>
		public ReferenceVectorSlice(FdoCache cache, ICmObject obj, int flid)
			: this(new VectorReferenceLauncher(), cache, obj, flid)
		{
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
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				var vrl = Control as VectorReferenceLauncher;
				if (vrl != null)
				{
					vrl.ViewSizeChanged -= OnViewSizeChanged;
					var view = (VectorReferenceView)vrl.MainControl;
					view.ViewSizeChanged -= OnViewSizeChanged;
				}
			}

			// Dispose unmanaged resources here, whether disposing is true or false.

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		public override void FinishInit()
		{
			CheckDisposed();

			base.FinishInit();

			InitLauncher();
		}

		protected virtual void InitLauncher()
		{
			CheckDisposed();

			var vrl = (VectorReferenceLauncher)Control;
			vrl.Initialize(m_cache, m_obj, m_flid, m_fieldName, m_persistenceProvider, Mediator,
				DisplayNameProperty,
				BestWsName); // TODO: Get better default 'best ws'.
			vrl.ConfigurationNode = ConfigurationNode;
			vrl.ViewSizeChanged += OnViewSizeChanged;
			var view = (VectorReferenceView)vrl.MainControl;
			view.ViewSizeChanged += OnViewSizeChanged;
			// We don't want to be visible until later, since otherwise we get a temporary
			// display in the wrong place with the wrong size that serves only to annoy the
			// user.  See LT-1518 "The drawing of the DataTree for Lexicon/Advanced Edit draws
			// some initial invalid controls."  Becoming visible when we set the width and
			// height seems to delay things enough to avoid this visual clutter.
			vrl.Visible = false;
		}

		public bool OnMoveTargetDownInSequence(object args)
		{
			GetView().MoveItem(false);
			return true;
		}
		public virtual bool OnDisplayMoveTargetDownInSequence(object commandObject, ref UIItemDisplayProperties display)
		{
			bool visible;
			display.Enabled = GetView().CanMoveItem(false, out visible);
			display.Visible = visible;
			return true;
		}

		public bool OnMoveTargetUpInSequence(object args)
		{
			GetView().MoveItem(true);
			return true;
		}

		public virtual bool OnDisplayMoveTargetUpInSequence(object commandObject, ref UIItemDisplayProperties display)
		{
			bool visible;
			display.Enabled = GetView().CanMoveItem(true, out visible);
			display.Visible = visible;
			return true;
		}

		public bool OnAlphabeticalOrder(object args)
		{
			GetView().RemoveOrdering();
			return true;
		}
		public virtual bool OnDisplayAlphabeticalOrder(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Enabled = display.Visible = GetView().RootPropertySupportsVirtualOrdering();
			return true;
		}

		private VectorReferenceView GetView()
		{
			var vrl = Control as VectorReferenceLauncher;
			return vrl.MainControl as VectorReferenceView;
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged (e);
			if (Width == m_dxLastWidth)
				return;
			m_dxLastWidth = Width; // BEFORE doing anything, actions below may trigger recursive call.
			var vrl = (VectorReferenceLauncher)Control;
			var view = (VectorReferenceView)vrl.MainControl;
			view.PerformLayout();
			int h1 = view.RootBox.Height;
			int hNew = Math.Max(h1, ContainingDataTree.GetMinFieldHeight()) + 3;
			if (hNew != Height)
			{
				Height = hNew;
			}
		}

		public override void ShowSubControls()
		{
			CheckDisposed();
			base.ShowSubControls ();
			Control.Visible = true;
		}

		/// <summary>
		/// Handle changes in the size of the underlying view.
		/// </summary>
		protected void OnViewSizeChanged(object sender, FwViewSizeEventArgs e)
		{
			// For now, just handle changes in the height.
			var vrl = (VectorReferenceLauncher)Control;
			var view = (VectorReferenceView)vrl.MainControl;
			int hMin = ContainingDataTree.GetMinFieldHeight();
			int h1 = view.RootBox.Height;
			Debug.Assert(e.Height == h1);
			int hOld = TreeNode.Height;
			int hNew = Math.Max(h1, hMin) + 3;
			if (hNew != hOld)
			{
				// JohnT: why all these -1's?
				Height = hNew - 1;
				// JohnT: don't know why we need this, vrl is the slice's control and is supposed to
				// be docked to fill the slice. But if we don't do it, there are cases where
				// narrowing the window makes the slice higher but not the embedded control.
				// The tree node is also supposed to be docked, but again, if we don't do this
				// then the tree node doesn't fill the height of the window, and clicks at the
				// bottom of it may not work.
				TreeNode.Height = hNew - 1;
				vrl.Height = hNew - 1;
				// This seems to be really not needed, the view height is docked to the launcher's.
//				view.Height = hNew - 1;
			}
			if (Height != hNew - 1)
			{
				Height = hNew - 1;
			}
		}

//		// Overhaul Aug 05: want all Window backgrounds in Detail controls.
//		/// <summary>
//		/// This is passed the color that the XDE specified, if any, otherwise null.
//		/// The default is to use the normal window color for editable text.
//		/// Subclasses which know they should have a different default should
//		/// override this method, but normally should use the specified color if not
//		/// null.
//		/// </summary>
//		/// <param name="clr"></param>
//		public override void OverrideBackColor(String backColorName)
//		{
//			CheckDisposed();
//
//			if (this.Control == null)
//				return;
//			VectorReferenceLauncher vrl = (VectorReferenceLauncher)this.Control;
//			vrl.BackColor = System.Drawing.SystemColors.Control;
//		}

		public override void RegisterWithContextHelper ()
		{
			CheckDisposed();
			if (Control != null)
			{
				Mediator mediator = Mediator;
				if (mediator != null) // paranoia and unit testing
				{
					StringTable tbl = null;
					if (mediator.HasStringTable)
						tbl = mediator.StringTbl;
					string caption = XmlUtils.GetLocalizedAttributeValue(tbl,
						ConfigurationNode, "label", "");
					var vrl = (VectorReferenceLauncher)Control;
					mediator.SendMessage("RegisterHelpTargetWithId",
						new object[]{vrl.Controls[1], caption, HelpId}, false);
					mediator.SendMessage("RegisterHelpTargetWithId",
						new object[]{vrl.Controls[0], caption, HelpId, "Button"}, false);
				}
			}
		}
	}

	/// <summary>
	/// This class should be extended by any custom reference vector slices.
	/// </summary>
	public abstract class CustomReferenceVectorSlice : ReferenceVectorSlice
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CustomReferenceVectorSlice"/> class.
		/// </summary>
		/// <param name="control">The control.</param>
		protected CustomReferenceVectorSlice(Control control)
			: base(control)
		{
		}

		public override void FinishInit()
		{
			CheckDisposed();

			SetFieldFromConfig();
			base.FinishInit();
		}
	}
	public class ReferenceVectorDisabledSlice : ReferenceVectorSlice
	{
		public ReferenceVectorDisabledSlice(FdoCache cache, ICmObject obj, int flid)
			: base(cache, obj, flid)
		{
		}
		public override void FinishInit()
		{
			CheckDisposed();
			base.FinishInit();
			var arl = (VectorReferenceLauncher)Control;
			var view = (VectorReferenceView)arl.MainControl;
			view.FinishInit(ConfigurationNode);
		}
	}

}
