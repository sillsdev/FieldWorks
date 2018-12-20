// Copyright (c) 2005-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Windows.Forms;
using SIL.FieldWorks.Common.RootSites;
using SIL.Xml;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	/// A tree control item where the embedded form is a View (specifically
	/// SIL.FieldWorks.Common.Framework.RootSite).
	/// </summary>
	internal class ViewSlice : Slice
	{
		/// <summary />
		public ViewSlice()
		{
		}

		/// <summary />
		public ViewSlice(SimpleRootSite ctrlT) : base(ctrlT)
		{
			InternalInitialize();
		}

		protected void InternalInitialize()
		{
			RootSite.Enter += ViewSlice_Enter;
		}

		#region IDisposable override

		/// <inheritdoc />
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
				// Dispose managed resources here.
				SimpleRootSite rs = RootSite;
				if (rs != null)
				{
					rs.LayoutSizeChanged -= HandleLayoutSizeChanged;
					rs.Enter -= ViewSlice_Enter;
					rs.SizeChanged -= rs_SizeChanged;
				}
			}

			// Dispose unmanaged resources here, whether disposing is true or false.

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		/// <summary />
		public override Control Control
		{
			get
			{
				return base.Control;
			}
			set
			{
				base.Control = value;
				SimpleRootSite rs = RootSite;
				// Don't allow it to lay out until we have a realistic size, while the DataTree is
				// actually being laid out.
				rs.AllowLayout = false;
				// Embedded forms should not do their own scrolling. Rather we resize them as needed, and scroll the whole
				// DE view.
				rs.AutoScroll = false;
				rs.LayoutSizeChanged += HandleLayoutSizeChanged;
				rs.SizeChanged += rs_SizeChanged;

				// This is usually done by the DataTree method that creates and initializes slices.
				// However, for most view slices doing it before the control is set does no good.
				// On the other hand, we don't want to do it during the constructor, and have it done again
				// unnecessarily by this method (which the constructor calls).
				// In any case we can't do it until our node is set.
				// So, do it only if the node is known.
				if (ConfigurationNode != null)
				{
					OverrideBackColor(XmlUtils.GetOptionalAttributeValue(ConfigurationNode, "backColor"));
				}
			}
		}

		private void rs_SizeChanged(object sender, EventArgs e)
		{
			if (RootSite.AllowLayout)
			{
				SetHeightFromRootBox(RootSite);
			}
		}

		/// <summary>
		/// Get the rootsite. It's important to use this method to get the rootsite, not to
		/// assume that the control is a rootsite, because some classes override and insert
		/// another layer of control, with the root site being a child.
		/// </summary>
		public virtual RootSite RootSite => (RootSite)Control;

		/// <summary />
		public void HandleLayoutSizeChanged(object sender, EventArgs ea)
		{
			SetHeightFromRootBox(RootSite);
			ContainingDataTree?.PerformLayout();
		}

		/// <summary />
		public override void Install(DataTree parentDataTree)
		{
			// Sometimes we get a spurious "out of memory" error while trying to create a handle for the
			// RootSite if its cache isn't set before we add it to its parent.
			var rs = RootSite;
			rs.Cache = Cache;
			// JT: seems to actually cause a problem if we replace it with itself. RootSite probably needs a fix.
			if (rs.StyleSheet != parentDataTree.StyleSheet)
			{
				rs.StyleSheet = parentDataTree.StyleSheet;
			}
			base.Install(parentDataTree);
			rs.SetAccessibleName(Label);
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			// Skip handling this, if the DataTree hasn't
			// set the official width using SetWidthForDataTreeLayout
			if (!m_widthHasBeenSetByDataTree)
			{
				return;
			}
			if (Cache == null || DesignMode)
			{
				return;
			}
			base.OnSizeChanged(e);
		}

		protected internal override void SetWidthForDataTreeLayout(int width)
		{
			if (Width == width)
			{
				return; // Nothing to do.
			}
			base.SetWidthForDataTreeLayout(width);
			var rs = RootSite;
			if (!rs.AllowLayout)
			{
				return;
			}
			// already laid out at some other width, need to do it again right now so
			// we can get an accurate height.
			rs.PerformLayout();
			SetHeightFromRootBox(rs);
		}

		/// <summary>
		/// A view slice is not 'real' until BecomeRealInPlace is called so it gets laid out.
		/// </summary>
		public override bool IsRealSlice => RootSite.AllowLayout;

		/// <summary>
		/// Some 'unreal' slices can become 'real' (ready to actually display) without
		/// actually replacing themselves with a different object. Such slices override
		/// this method to do whatever is needed and then answer true. If a slice
		/// answers false to IsRealSlice, this is tried, and if it returns false,
		/// then BecomeReal is called.
		/// </summary>
		public override bool BecomeRealInPlace()
		{
			var rs = RootSite;
			if (rs.RootBox == null)
			{
				var dummy = rs.Handle; // This typically gets the root box created, so setting AllowLayout can lay it out.
			}
			rs.AllowLayout = true; // also does PerformLayout.
			SetHeightFromRootBox(rs);
			return true;
		}

		private void SetHeightFromRootBox(RootSite rs)
		{
			if (rs.RootBox == null)
			{
				return;
			}
			var widthOrig = rs.Width;
			Height = Math.Max(LabelHeight, DesiredHeight(rs));  // Allow it to be the height it wants.
			if (widthOrig == rs.Width)
			{
				return;
			}
			// If the rootsite width changes, we need to layout again.  See LT-6156.  (This is too much
			// like a band-aid, but it's taken me 3 days to figure even this much out!)
			rs.AllowLayout = true;
			Height = Math.Max(LabelHeight, DesiredHeight(rs));
		}

		/// <summary>
		/// The height that the slice would ideally be to accommodate the rootsite.
		/// </summary>
		protected virtual int DesiredHeight(RootSite rs)
		{
			return rs.RootBox.Height;
		}

		/// <summary />
		private void ViewSlice_Enter(object sender, EventArgs e)
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
			base.AboutToDiscard();
			var rs = RootSite;
			rs?.AboutToDiscard();
		}
	}
}