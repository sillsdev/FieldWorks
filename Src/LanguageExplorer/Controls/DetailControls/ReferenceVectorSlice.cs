// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Windows.Forms;
using SIL.LCModel;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	/// Class that can edit a field that is a sequence of object references, typically by launching a chooser.
	/// Also a base class for more specialized editors.
	/// Control should be a (subclass of) VectorReferenceLauncher.
	/// </summary>
	internal class ReferenceVectorSlice : ReferenceSlice
	{
		int m_dxLastWidth; // remember width when OnSizeChanged called.

		/// <summary>
		/// Initializes a new instance of the <see cref="ReferenceVectorSlice"/> class.
		/// Used by custom slices that extend this class.
		/// </summary>
		protected ReferenceVectorSlice(Control control)
			: base(control)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ReferenceVectorSlice"/> class.
		/// Used by slices that extend this class.
		/// </summary>
		protected ReferenceVectorSlice(Control control, LcmCache cache, ICmObject obj, int flid)
			: base(control, cache, obj, flid)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ReferenceVectorSlice"/> class.
		/// </summary>
		public ReferenceVectorSlice(LcmCache cache, ICmObject obj, int flid)
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
			// Must not be run more than once.
			if (IsDisposed)
			{
				return;
			}

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
			base.FinishInit();

			InitLauncher();
		}

		protected virtual void InitLauncher()
		{
			var vrl = (VectorReferenceLauncher)Control;
			vrl.Initialize(Cache, MyCmObject, m_flid, m_fieldName, PersistenceProvider, DisplayNameProperty, BestWsName); // TODO: Get better default 'best ws'.
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

#if RANDYTODO
		public virtual bool OnDisplayMoveTargetDownInSequence(object commandObject, ref UIItemDisplayProperties display)
		{
			bool visible;
			display.Enabled = GetView().CanMoveItem(false, out visible);
			display.Visible = visible;
			return true;
		}
#endif

		public bool OnMoveTargetUpInSequence(object args)
		{
			GetView().MoveItem(true);
			return true;
		}

#if RANDYTODO
		public virtual bool OnDisplayMoveTargetUpInSequence(object commandObject, ref UIItemDisplayProperties display)
		{
			bool visible;
			display.Enabled = GetView().CanMoveItem(true, out visible);
			display.Visible = visible;
			return true;
		}
#endif

		public bool OnAlphabeticalOrder(object args)
		{
			GetView().RemoveOrdering();
			return true;
		}

#if RANDYTODO
		public virtual bool OnDisplayAlphabeticalOrder(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Enabled = display.Visible = GetView().RootPropertySupportsVirtualOrdering();
			return true;
		}
#endif

		private VectorReferenceView GetView()
		{
			return ((VectorReferenceLauncher)Control).MainControl as VectorReferenceView;
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged (e);
			if (Width == m_dxLastWidth)
			{
				return;
			}
			m_dxLastWidth = Width; // BEFORE doing anything, actions below may trigger recursive call.
			var view = (VectorReferenceView)((VectorReferenceLauncher)Control).MainControl;
			view.PerformLayout();
			var h1 = view.RootBox.Height;
			var hNew = Math.Max(h1, ContainingDataTree.GetMinFieldHeight()) + 3;
			if (hNew != Height)
			{
				Height = hNew;
			}
		}

		public override void ShowSubControls()
		{
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
			var hMin = ContainingDataTree.GetMinFieldHeight();
			var h1 = view.RootBox.Height;
			Debug.Assert(e.Height == h1);
			var hOld = TreeNode.Height;
			var hNew = Math.Max(h1, hMin) + 3;
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
				// The view height is docked to the launcher's.
			}
			if (Height != hNew - 1)
			{
				Height = hNew - 1;
			}
		}

		public override void RegisterWithContextHelper ()
		{
			if (Control == null)
			{
				return;
			}
			if (Publisher != null)
			{
#if RANDYTODO
// TODO: Skip it for now, and figure out what to do with those context menus
					string caption = StringTable.Table.LocalizeAttributeValue(XmlUtils.GetOptionalAttributeValue(ConfigurationNode, "label", ""));
					var vrl = (VectorReferenceLauncher)Control;
					Publisher.Publish("RegisterHelpTargetWithId", new object[]{vrl.Controls[1], caption, HelpId});
					Publisher.Publish("RegisterHelpTargetWithId", new object[]{vrl.Controls[0], caption, HelpId, "Button"});
#endif
			}
		}
	}
}