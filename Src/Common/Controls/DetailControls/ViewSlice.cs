using System;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.Utils;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// A tree control item where the embedded form is a View (specifically
	/// SIL.FieldWorks.Common.Framework.RootSite).
	/// </summary>
	public class ViewSlice: Slice
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ViewSlice()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="ctrlT"></param>
		/// ------------------------------------------------------------------------------------
		public ViewSlice(SimpleRootSite ctrlT): base(ctrlT)
		{
			InternalInitialize();
		}

		protected void InternalInitialize()
		{
			RootSite.Enter += new EventHandler(ViewSlice_Enter);
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
				SimpleRootSite rs = RootSite;
				if (rs != null)
				{
					rs.LayoutSizeChanged -= new EventHandler(this.HandleLayoutSizeChanged);
					rs.Enter -= new EventHandler(ViewSlice_Enter);
					rs.SizeChanged -= new EventHandler(rs_SizeChanged);
				}
			}

			// Dispose unmanaged resources here, whether disposing is true or false.

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override Control Control
		{
			get
			{
				CheckDisposed();
				return base.Control;
			}
			set
			{
				CheckDisposed();
				base.Control = value;
				SimpleRootSite rs = RootSite;
				// Don't allow it to lay out until we have a realistic size, while the DataTree is
				// actually being laid out.
				rs.AllowLayout = false;

				// Embedded forms should not do their own scrolling. Rather we resize them as needed, and scroll the whole
				// DE view.
				rs.AutoScroll = false;
				rs.LayoutSizeChanged += new EventHandler(this.HandleLayoutSizeChanged);
				rs.SizeChanged += new EventHandler(rs_SizeChanged);


				// This is usually done by the DataTree method that creates and initializes slices.
				// However, for most view slices doing it before the control is set does no good.
				// On the other hand, we don't want to do it during the constructor, and have it done again
				// unnecessarily by this method (which the constructor calls).
				// In any case we can't do it until our node is set.
				// So, do it only if the node is known.
				if (ConfigurationNode != null)
					OverrideBackColor(XmlUtils.GetOptionalAttributeValue(ConfigurationNode, "backColor"));
			}
		}

		void rs_SizeChanged(object sender, EventArgs e)
		{
			if (RootSite.AllowLayout)
				SetHeightFromRootBox(RootSite);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the rootsite. It's important to use this method to get the rootsite, not to
		/// assume that the control is a rootsite, because some classes override and insert
		/// another layer of control, with the root site being a child.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual RootSite RootSite
		{
			get
			{
				CheckDisposed();
				return (RootSite)this.Control;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="ea"></param>
		/// ------------------------------------------------------------------------------------
		public void HandleLayoutSizeChanged(object sender, EventArgs ea)
		{
			CheckDisposed();
			SetHeightFromRootBox(RootSite);
			if (ContainingDataTree != null) // can happen, e.g., during install slice.
				ContainingDataTree.PerformLayout();
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="parent"></param>
		public override void Install(DataTree parent)
		{
			CheckDisposed();
			// Sometimes we get a spurious "out of memory" error while trying to create a handle for the
			// RootSite if its cache isn't set before we add it to its parent.
			RootSite rs = RootSite;
			rs.Cache = Cache;
			// JT: seems to actually cause a problem if we replace it with itself. RootSite probably needs a fix.
			if (rs.StyleSheet != parent.StyleSheet)
				rs.StyleSheet = parent.StyleSheet;

			base.Install(parent);

			rs.SetAccessibleName(this.Label);
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			// Skip handling this, if the DataTree hasn't
			// set the official width using SetWidthForDataTreeLayout
			if (!m_widthHasBeenSetByDataTree)
				return;

			if (m_cache == null || DesignMode)
				return;

			base.OnSizeChanged(e);
		}

		protected internal override void SetWidthForDataTreeLayout(int width)
		{
			CheckDisposed();

			if (Width == width)
				return; // Nothing to do.

			base.SetWidthForDataTreeLayout(width);

			RootSite rs = this.RootSite;
			if (rs.AllowLayout)
			{
				// already laid out at some other width, need to do it again right now so
				// we can get an accurate height.
				rs.PerformLayout();
				SetHeightFromRootBox(rs);
			}
		}

		/// <summary>
		/// A view slice is not 'real' until BecomeRealInPlace is called so it gets laid out.
		/// </summary>
		public override bool IsRealSlice
		{
			get
			{
				CheckDisposed();
				return RootSite.AllowLayout;
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
		public override bool BecomeRealInPlace()
		{
			CheckDisposed();
			RootSite rs = this.RootSite;
			if (rs.RootBox == null)
			{
#pragma warning disable 0219 // error CS0219: The variable ... is assigned but its value is never used
				IntPtr dummy = rs.Handle; // This typically gets the root box created, so setting AllowLayout can lay it out.
#pragma warning restore 0219
			}
			rs.AllowLayout = true; // also does PerformLayout.
			SetHeightFromRootBox(rs);
			return true;
		}

		private void SetHeightFromRootBox(RootSite rs)
		{
			if (rs.RootBox != null)
			{
				//Debug.WriteLine(String.Format("ViewSlice.SetHeightFromRootBox(): orig rs.Size = {0}, this.Size = {1}",
				//    rs.Size.ToString(), this.Size.ToString()));
				int widthOrig = rs.Width;
				this.Height = Math.Max(LabelHeight, DesiredHeight(rs));  // Allow it to be the height it wants.
				//Debug.WriteLine(String.Format("ViewSlice.SetHeightFromRootBox(): new rs.Size = {0}, this.Size = {1}",
				//    rs.Size.ToString(), this.Size.ToString()));
				if (widthOrig != rs.Width)
				{
					// If the rootsite width changes, we need to layout again.  See LT-6156.  (This is too much
					// like a band-aid, but it's taken me 3 days to figure even this much out!)
					rs.AllowLayout = true;
					this.Height = Math.Max(LabelHeight, DesiredHeight(rs));
				//    Debug.WriteLine(String.Format("ViewSlice.SetHeightFromRootBox(): final rs.Size = {0}, this.Size = {1}",
				//        rs.Size.ToString(), this.Size.ToString()));
				}
			}
		}

		/// <summary>
		/// The height that the slice would ideally be to accommodate the rootsite.
		/// </summary>
		/// <param name="rs"></param>
		/// <returns></returns>
		protected virtual int DesiredHeight(RootSite rs)
		{
			return rs.RootBox.Height;
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void ViewSlice_Enter(object sender, System.EventArgs e)
		{
			RootSite.Focus();
			ContainingDataTree.ActiveControl = RootSite;
		}

		/// <summary>
		/// Somehow a slice (I think one that has never scrolled to become visible?)
		/// can get an OnLoad message for its view in the course of deleting it from the
		/// parent controls collection. This can be bad (at best it's a waste of time
		/// to do the Layout in the OnLoad, but it can be actively harmful if the object
		/// the view is displaying has been deleted). So suppress it.
		/// </summary>
		public override void AboutToDiscard()
		{
			CheckDisposed();
			base.AboutToDiscard ();
			RootSite rs = RootSite;
			if (rs != null)
				rs.AboutToDiscard();
		}
	}
}
